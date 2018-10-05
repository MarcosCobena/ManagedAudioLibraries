using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SoundExporter
{
    public static class WavConverter
    {
        const string InvalidPathMessage = "Path cannot be null or white spaces";

        public static bool TryConvert(
            string inputPath, string outputPath,
            SampleRate? sampleRate = null, ChannelFormat? channelFormat = null, BitRate? bitRate = null)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                throw new ArgumentException(InvalidPathMessage, nameof(inputPath));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException(InvalidPathMessage, nameof(outputPath));
            }

            if (!sampleRate.HasValue && !channelFormat.HasValue && !bitRate.HasValue)
            {
                return false;
            }

            var outputFileName = Path.GetFileNameWithoutExtension(outputPath);
            var isAlreadyConverterToBitRateHigh = false;

            var isSampleRateSuccess = TryConvert(
                inputPath, out string intermediateSampleRatePath, sampleRate, outputFileName,
                ref isAlreadyConverterToBitRateHigh);

            var isChannelFormatSuccess = TryConvert(
                intermediateSampleRatePath, out string intermediateChannelFormatPath, channelFormat, outputFileName);

            // Bit rate conversion must be the last one as any other will turn output back to 16
            var isBitRateSuccess = TryConvert(
                intermediateChannelFormatPath, out string intermediateBitRatePath, bitRate, outputFileName,
                isAlreadyConverterToBitRateHigh);

            bool isEverythingOK;

            if (isSampleRateSuccess && isChannelFormatSuccess && isBitRateSuccess)
            {
                File.Copy(intermediateBitRatePath, outputPath, true);
                isEverythingOK = true;
            }
            else
            {
                isEverythingOK = false;
            }

#if !DEBUG
            DeleteIntermediateFiles(intermediateSampleRatePath, intermediateChannelFormatPath, intermediateBitRatePath);
#endif

            return isEverythingOK;
        }

#if !DEBUG
        static void DeleteIntermediateFiles(params string[] paths)
        {
            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
#endif

        static bool TryConvert(
            string inputPath, out string intermediateChannelFormatPath, ChannelFormat? channelFormat, 
            string outputFileName)
        {
            if (channelFormat.HasValue)
            {
                using (var reader = new AudioFileReader(inputPath))
                {
                    intermediateChannelFormatPath = $"{outputFileName}-intermediate-channelformat.wav";

                    if (channelFormat.Value == ChannelFormat.Mono)
                    {
                        if (reader.WaveFormat.Channels == 2)
                        {
                            var resampler = new StereoToMonoSampleProvider(reader)
                            {
                                LeftVolume = 0,
                                RightVolume = 1
                            };
                            WaveFileWriter.CreateWaveFile16(intermediateChannelFormatPath, resampler);
                        }
                        else
                        {
                            // Do nothing, already mono
                            File.Copy(inputPath, intermediateChannelFormatPath, true);
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
                            WaveFileWriter.CreateWaveFile16(intermediateChannelFormatPath, resampler);
                        }
                        else
                        {
                            // Do nothing, already stereo
                            File.Copy(inputPath, intermediateChannelFormatPath, true);
                        }
                    }
                }
            }
            else
            {
                intermediateChannelFormatPath = inputPath;
            }

            return true;
        }

        private static bool TryConvert(
            string inputPath, out string intermediateBitRatePath, BitRate? bitRate, string outputFileName, 
            bool isAlreadyConverterToBitRateHigh)
        {
            if (bitRate.HasValue)
            {
                using (var reader = new AudioFileReader(inputPath))
                {
                    intermediateBitRatePath = $"{outputFileName}-intermediate-bitrate.wav";
                    var originalFormat = reader.WaveFormat;

                    if (bitRate.Value == BitRate.High)
                    {
                        if (!isAlreadyConverterToBitRateHigh)
                        {
                            var resampler = new WdlResamplingSampleProvider(reader, originalFormat.SampleRate);
                            WaveFileWriter.CreateWaveFile16(intermediateBitRatePath, resampler);
                        }
                        else
                        {
                            intermediateBitRatePath = inputPath;
                        }
                    }
                    else // low
                    {
                        var resampler = new WdlResamplingSampleProvider(reader, originalFormat.SampleRate);
                        WaveFileWriter.CreateWaveFile16(intermediateBitRatePath, resampler);

                        using (var pcm16Stream = File.OpenRead(intermediateBitRatePath))
                        using (var memoryStream = ConvertPCM16ToPCM8(pcm16Stream))
                        {
                            var actualBitRate = BitRateHelper.GetBitRate(BitRate.Low);
                            var waveFormat = new WaveFormat(
                                originalFormat.SampleRate, actualBitRate, originalFormat.Channels);

                            using (var wavStream = new RawSourceWaveStream(memoryStream, waveFormat))
                            {
                                WaveFileWriter.CreateWaveFile(intermediateBitRatePath, wavStream);
                            }
                        }
                    }
                }
            }
            else
            {
                intermediateBitRatePath = inputPath;
            }

            return true;
        }

        static bool TryConvert(
            string inputPath, out string intermediateSampleRatePath, SampleRate? sampleRate, string outputFileName, 
            ref bool isAlreadyConverterToBitRateHigh)
        {
            if (sampleRate.HasValue)
            {
                int actualSampleRate = SampleRateHelper.GetSampleRate(sampleRate.Value);

                using (var reader = new AudioFileReader(inputPath))
                {
                    intermediateSampleRatePath = $"{outputFileName}-intermediate-samplerate.wav";
                    var resampler = new WdlResamplingSampleProvider(reader, actualSampleRate);
                    WaveFileWriter.CreateWaveFile16(intermediateSampleRatePath, resampler);
                    isAlreadyConverterToBitRateHigh = true;
                }
            }
            else
            {
                intermediateSampleRatePath = inputPath;
            }

            return true;
        }

        static unsafe MemoryStream ConvertPCM16ToPCM8(Stream reader)
        {
            var length = (int)(reader.Length - reader.Position);
            var outputStream = new MemoryStream(length / sizeof(short));
            var buffer = new byte[sizeof(short)];

            fixed (byte* bufferRef = &buffer[0])
            {
                var sampleCount = length / sizeof(short);
                var sample = (short*)bufferRef;

                for (int i = 0; i < sampleCount; i++)
                {
                    reader.Read(buffer, 0, buffer.Length);
                    var value = (byte)((*sample + short.MaxValue) >> 8);
                    outputStream.WriteByte(value);
                }
            }

            outputStream.Seek(0, SeekOrigin.Begin);

            return outputStream;
        }
    }
}
