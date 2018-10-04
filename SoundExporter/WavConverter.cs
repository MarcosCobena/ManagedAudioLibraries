using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SoundExporter
{
    public static class WavConverter
    {
        public static bool TryConvert(
            string inputPath, string outputPath, 
            SampleRate? sampleRate = null, BitRate? bitRate = null, ChannelFormat? channelFormat = null)
        {
            var isAlreadyConverterToBitRateHigh = false;

            if (sampleRate.HasValue)
            {
                int actualSampleRate = SampleRateHelper.GetSampleRate(sampleRate.Value);

                using (var reader = new AudioFileReader(inputPath))
                {
                    var resampler = new WdlResamplingSampleProvider(reader, actualSampleRate);
                    WaveFileWriter.CreateWaveFile16(outputPath, resampler);
                    isAlreadyConverterToBitRateHigh = true;
                }
            }

            if (bitRate.HasValue)
            {
                using (var reader = new AudioFileReader(inputPath))
                {
                    var originalFormat = reader.WaveFormat;

                    if (bitRate.Value == BitRate.High)
                    {
                        if (!isAlreadyConverterToBitRateHigh)
                        {
                            var resampler = new WdlResamplingSampleProvider(reader, originalFormat.SampleRate);
                            WaveFileWriter.CreateWaveFile16(outputPath, resampler);
                        }
                    }
                    else // low
                    {
                        var resampler = new WdlResamplingSampleProvider(reader, originalFormat.SampleRate);
                        WaveFileWriter.CreateWaveFile16(outputPath, resampler);
                        var pcm16Stream = File.OpenRead(outputPath);
                        byte[] pcm8Bytes = new byte[pcm16Stream.Length];
                        ConvertPCM16ToPCM8(pcm16Stream, pcm8Bytes, 0, pcm8Bytes.Length);

                        using (var memoryStream = new MemoryStream(pcm8Bytes))
                        {
                            var actualBitRate = BitRateHelper.GetBitRate(BitRate.Low);
                            var waveFormat = new WaveFormat(
                                originalFormat.SampleRate, actualBitRate, originalFormat.Channels);

                            using (var wavStream = new RawSourceWaveStream(memoryStream, waveFormat))
                            {
                                WaveFileWriter.CreateWaveFile(outputPath, wavStream);
                            }
                        }
                    }
                }
            }

            if (channelFormat.HasValue)
            {
                using (var reader = new AudioFileReader(inputPath))
                {
                    if (channelFormat.Value == ChannelFormat.Mono)
                    {
                        if (reader.WaveFormat.Channels == 2)
                        {
                            var resampler = new StereoToMonoSampleProvider(reader)
                            {
                                LeftVolume = 0,
                                RightVolume = 1
                            };
                            WaveFileWriter.CreateWaveFile16(outputPath, resampler);
                        }
                        else
                        {
                            // Do nothing, already mono
                        }
                    }
                    else // stereo
                    {
                        if (reader.WaveFormat.Channels == 1)
                        {
                            var resampler = new MonoToStereoSampleProvider(reader)
                            {
                                LeftVolume = 0.5f,
                                RightVolume = 0.5f
                            };
                            WaveFileWriter.CreateWaveFile16(outputPath, resampler);
                        }
                        else
                        {
                            // Do nothing, already stereo
                            File.Copy(inputPath, outputPath, true);
                        }
                    }
                }
            }

            return true;
        }

        static unsafe int ConvertPCM16ToPCM8(Stream reader, byte[] data, int offset, int count)
        {
            if (offset + count > data.Length)
            {
                throw new ArgumentException();
            }

            // Sample count includes both channels if stereo
            var sampleCount = (count / sizeof(byte));
            var tmp = new byte[sizeof(short)];
            fixed (byte* dst = &data[offset])
            {
                fixed (byte* src = &tmp[0])
                {
                    var f = (short*)src;
                    var d = dst;
                    for (int i = 0; i < sampleCount; i++)
                    {
                        reader.Read(tmp, 0, tmp.Length);
                        *d++ = (byte)((*f + short.MaxValue) >> 8);
                    }
                }
            }

            return sampleCount * sizeof(byte);
        }
    }
}
