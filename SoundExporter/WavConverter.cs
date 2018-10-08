using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SoundExporter
{
    public static class WavConverter
    {
        const string InvalidPathMessage = "Path cannot be null or white spaces";

        /// <summary>
        /// Tries to convert <paramref name="inputPath"/> with passed params, saving it at 
        /// <paramref name="outputPath"/>. This method is thread-safe.
        /// 
        /// Supported input formats: 8, 16, 32 and 64 bits PCM
        /// Unsupported ones: A-Lau and Mu-Lau PCM
        /// </summary>
        /// <returns><c>true</c>, if conversion was successful, <c>false</c> otherwise.</returns>
        /// <param name="inputPath">Input path.</param>
        /// <param name="outputPath">Output path.</param>
        /// <param name="sampleRate">Sample rate.</param>
        /// <param name="channelFormat">Channel format.</param>
        /// <param name="bitRate">Bit rate.</param>
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

            var isFormatUnsupported = false;
            var isNeededNativeDependency = false;
            Exception innerException = null;

            try
            {
                using (var reader = new WaveFileReader(inputPath))
                {
                    var format = reader.WaveFormat;

                    if (format.Encoding == WaveFormatEncoding.ALaw || format.Encoding == WaveFormatEncoding.MuLaw)
                    {
                        isFormatUnsupported = true;
                    }
                }
            }
            catch (DllNotFoundException exception)
            {
                isNeededNativeDependency = true;
                innerException = exception;
            }

            if (isFormatUnsupported || isNeededNativeDependency)
            {
                throw new InvalidDataException(
                    "Input file format is not supported. Please, convert it previously to any supported one.", 
                    innerException);
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
                        using (var memoryStream = PCMConverter.ConvertPCM16ToPCM8(pcm16Stream))
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
    }
}
