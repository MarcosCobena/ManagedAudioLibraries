using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using MP3Sharp;
using NAudio.Wave;
using NVorbis;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

namespace OpenALDemo
{
    class Program
    {
        static readonly TimeSpan _delay = TimeSpan.FromSeconds(1);

        const string AudioFilesPathFormat = "bin/Debug/netcoreapp2.1/{0}";
        const int BuffersCount = 2;
        const int BufferSize = 1 * 1024 * 1024;

        // Based on: https://gist.github.com/kamiyaowl/32fb397e0141c65792e1
        static void Main(string[] args)
        {
            PlayOGG("178172__deitzis__deitzis.ogg"); // OK

            Thread.Sleep(_delay);

            PlayMP3("425556__planetronik__rock-808-beat.mp3"); // OK

            Thread.Sleep(_delay);

            PlayMP3("Me_&_U_(Cup_&_String_2_Step_Mix)___Free_Download__.mp3");

            Thread.Sleep(_delay);

            // 24 bits seem difficult to handle with OpenAL:
            // http://openal.996291.n3.nabble.com/24Bit-Audio-Support-td2182.html
            //PlayWav("182316__il112__backspin.wav"); // KO; this' actually 24 bits

            PlayWav("182316__il112__backspin_32bits.wav"); // OK
        }

        static void Dispose(ref IntPtr device, ref ContextHandle context)
        {
            if (context != ContextHandle.Zero)
            {
                Alc.MakeContextCurrent(ContextHandle.Zero);
                Alc.DestroyContext(context);
            }

            context = ContextHandle.Zero;

            if (device != IntPtr.Zero)
            {
                Alc.CloseDevice(device);
            }

            device = IntPtr.Zero;
        }

        static int[] Initialize(out IntPtr device, out ContextHandle context, out int source)
        {
            device = Alc.OpenDevice(null);
            context = Alc.CreateContext(device, (int[])null);
            Alc.MakeContextCurrent(context);

            var buffers = AL.GenBuffers(BuffersCount);
            AL.GenSources(1, out source);

            return buffers;
        }

        static void PlayAndDispose(Stream reader, int[] buffers, int source, int sampleRate, 
                                   ALFormat format = ALFormat.StereoFloat32Ext)
        {
            var totalBytesRead = ReadBuffersAndEnqueueThem(reader, buffers, source, sampleRate, format);
            AL.SourcePlay(source);

            Trace.WriteLine("Playing...");

            ALSourceState sourceState;

            do
            {
                Thread.Sleep(20);

                AL.GetSource(source, ALGetSourcei.BuffersProcessed, out int buffersProcessed);

                if (buffersProcessed > 0 && reader.Position < reader.Length)
                {
                    var unqueuedBuffers = AL.SourceUnqueueBuffers(source, buffersProcessed);

                    Trace.WriteLine(
                        $"- Unqueued {unqueuedBuffers.Length} buffers for {reader.Length - reader.Position} B left");

                    totalBytesRead += ReadBuffersAndEnqueueThem(reader, unqueuedBuffers, source, sampleRate, format);
                }

                sourceState = AL.GetSourceState(source);
            }
            while (sourceState == ALSourceState.Playing);

            Trace.WriteLine($"Stop ({totalBytesRead} B read)");

            reader.Dispose();
            reader = null;
        }

        static int ReadBuffersAndEnqueueThem(Stream reader, int[] buffers, int source, int sampleRate, ALFormat format)
        {
            var bytesRead = 0;
            var totalBytesRead = 0;
            var readBuffer = new byte[BufferSize];
            var currentBufferIndex = 0;

            while (currentBufferIndex < buffers.Length &&
                   (bytesRead = reader.Read(readBuffer, 0, readBuffer.Length)) > 0)
            {
                var currentBuffer = buffers[currentBufferIndex];

                AL.BufferData(currentBuffer, format, readBuffer, bytesRead, sampleRate);
                AL.SourceQueueBuffer(source, currentBuffer);

                Trace.WriteLine(
                    $"+ Queued buffer {currentBuffer} with {bytesRead} B, stream position at {reader.Position}");

                currentBufferIndex++;
                totalBytesRead += bytesRead;
            }

            Trace.WriteLine($"~ Queued a total of {totalBytesRead} B");

            return totalBytesRead;
        }

        static void PlayOGG(string filename)
        {
            var buffers = Initialize(out IntPtr device, out ContextHandle context, out int source);

            var vorbis = new VorbisReader(string.Format(AudioFilesPathFormat, filename));
            var stream = new MemoryStream();
            var samples = new float[vorbis.Channels * vorbis.SampleRate];
            var samplesRead = 0;

            while ((samplesRead = vorbis.ReadSamples(samples, 0, samples.Length)) > 0)
            {
                var readBuffer = new byte[samplesRead * 4];
                Buffer.BlockCopy(samples, 0, readBuffer, 0, readBuffer.Length);

                stream.Write(readBuffer, 0, readBuffer.Length);
            }

            stream.Position = 0;

            PlayAndDispose(stream, buffers, source, vorbis.SampleRate);

            vorbis.Dispose();
            vorbis = null;

            Dispose(ref device, ref context);
        }

        static void PlayMP3(string filename)
        {
            var buffers = Initialize(out IntPtr device, out ContextHandle context, out int source);

            var reader = new MP3Stream(string.Format(AudioFilesPathFormat, filename));
            PlayAndDispose(reader, buffers, source, reader.Frequency, ALFormat.Stereo16);

            Dispose(ref device, ref context);
        }

        static void PlayWav(string filename)
        {
            var buffers = Initialize(out IntPtr device, out ContextHandle context, out int source);

            var reader = new WaveFileReader(string.Format(AudioFilesPathFormat, filename));
            PlayAndDispose(reader, buffers, source, reader.WaveFormat.SampleRate);

            Dispose(ref device, ref context);
        }
    }
}
