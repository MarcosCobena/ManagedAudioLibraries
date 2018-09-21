using System;
using System.Diagnostics;
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

        // Based on: https://gist.github.com/kamiyaowl/32fb397e0141c65792e1
        static void Main(string[] args)
        {
            PlayOGG("178172__deitzis__deitzis.ogg"); // OK

            Thread.Sleep(_delay);

            PlayMP3("425556__planetronik__rock-808-beat.mp3"); // OK

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

        static void Initialize(out IntPtr device, out ContextHandle context, out uint buffer, out int source)
        {
            device = Alc.OpenDevice(null);
            context = Alc.CreateContext(device, (int[])null);
            Alc.MakeContextCurrent(context);

            AL.GenBuffer(out buffer);
            AL.GenSources(1, out source);
        }

        static void Play<TBuffer>(
            uint buffer, int source, int sampleRate, TBuffer[] readBuffer, ALFormat format = ALFormat.StereoFloat32Ext) 
            where TBuffer : struct
        {
            AL.BufferData((int)buffer, format, readBuffer, readBuffer.Length, sampleRate);
            AL.Source(source, ALSourcei.Buffer, (int)buffer);

            AL.SourcePlay(source);
            Trace.Write("Playing...");

            SleepThreadWhilePlaying(source);
        }

        static void PlayOGG(string filename)
        {
            Initialize(out IntPtr device, out ContextHandle context, out uint buffer, out int source);

            using (var vorbis = new VorbisReader(string.Format(AudioFilesPathFormat, filename)))
            {
                var samples = new float[vorbis.Channels * vorbis.SampleRate];
                vorbis.ReadSamples(samples, 0, samples.Length);
                var readBuffer = new byte[samples.Length * 4];
                Buffer.BlockCopy(samples, 0, readBuffer, 0, readBuffer.Length);

                Play(buffer, source, vorbis.SampleRate, readBuffer);
            }

            Dispose(ref device, ref context);
        }

        static void PlayMP3(string filename)
        {
            Initialize(out IntPtr device, out ContextHandle context, out uint buffer, out int source);

            using (var reader = new MP3Stream(string.Format(AudioFilesPathFormat, filename)))
            {
                var readBuffer = new byte[reader.Length];
                reader.Read(readBuffer, 0, readBuffer.Length);

                Play(buffer, source, reader.Frequency, readBuffer, ALFormat.Stereo16);
            }

            Dispose(ref device, ref context);
        }

        static void PlayWav(string filename)
        {
            Initialize(out IntPtr device, out ContextHandle context, out uint buffer, out int source);

            using (var reader = new WaveFileReader(string.Format(AudioFilesPathFormat, filename)))
            {
                var readBuffer = new byte[reader.Length];
                reader.Read(readBuffer, 0, (int)reader.Length);

                Play(buffer, source, reader.WaveFormat.SampleRate, readBuffer);
            }

            Dispose(ref device, ref context);
        }

        static void SleepThreadWhilePlaying(int source)
        {
            ALSourceState sourceState;

            do
            {
                Thread.Sleep(10);
                sourceState = AL.GetSourceState(source);
                Trace.Write(".");
            }
            while (sourceState == ALSourceState.Playing);

            Trace.Write(Environment.NewLine);
        }
    }
}
