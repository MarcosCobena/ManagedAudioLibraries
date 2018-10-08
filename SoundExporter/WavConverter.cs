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
        /// Converts <paramref name="inputPath"/> with passed params, saving it at 
        /// <paramref name="outputPath"/>. This method is thread-safe.
        /// 
        /// Supported input formats: 8, 16, 32 and 64 bits PCM
        /// Unsupported ones: A-Lau and Mu-Lau PCM
        /// </summary>
        /// <param name="inputPath">Input path.</param>
        /// <param name="outputPath">Output path.</param>
        /// <param name="sampleRate">Sample rate.</param>
        /// <param name="channelFormat">Channel format.</param>
        /// <param name="bitRate">Bit rate.</param>
        public static void Convert(
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

            CheckSupportedFormat(inputPath);

            if (!sampleRate.HasValue && !channelFormat.HasValue && !bitRate.HasValue)
            {
                return;
            }

            var outputDirectory = Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrWhiteSpace(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var outputFileName = Path.GetFileNameWithoutExtension(outputPath);
            var isAlreadyConvertedToBitRateHigh = false;

            Convert(
                inputPath, out string intermediateSampleRatePath, sampleRate, outputDirectory, outputFileName,
                ref isAlreadyConvertedToBitRateHigh);

            Convert(
                intermediateSampleRatePath, out string intermediateChannelFormatPath, channelFormat, 
                outputDirectory, outputFileName);

            // Bit rate conversion must be the last one as any other will turn output back to 16
            Convert(
                intermediateChannelFormatPath, out string intermediateBitRatePath, bitRate, 
                outputDirectory, outputFileName, isAlreadyConvertedToBitRateHigh);

            File.Copy(intermediateBitRatePath, outputPath, true);

#if !DEBUG
            DeleteIntermediateFiles(intermediateSampleRatePath, intermediateChannelFormatPath, intermediateBitRatePath);
#endif
        }

        static void CheckSupportedFormat(string inputPath)
        {
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

        static void Convert(
            string inputPath, out string intermediateChannelFormatPath, ChannelFormat? channelFormat, 
            string outputDirectory, string outputFileName)
        {
            if (!channelFormat.HasValue)
            {
                intermediateChannelFormatPath = inputPath;
                return;
            }

            using (var reader = new AudioFileReader(inputPath))
            {
                var intermediateChannelFormatFileName = $"{outputFileName}-intermediate-channelformat.wav";
                intermediateChannelFormatPath = Path.Combine(outputDirectory, intermediateChannelFormatFileName);

                if (reader.WaveFormat.Channels == 2 && channelFormat.Value == ChannelFormat.Mono)
                {
                    var resampler = new StereoToMonoSampleProvider(reader)
                    {
                        LeftVolume = 0,
                        RightVolume = 1
                    };
                    WaveFileWriter.CreateWaveFile16(intermediateChannelFormatPath, resampler);
                }
                else if (reader.WaveFormat.Channels == 1 && channelFormat.Value == ChannelFormat.Stereo)
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
                    // Do nothing, already same channel format
                    File.Copy(inputPath, intermediateChannelFormatPath, true);
                }
            }
        }

        private static void Convert(
            string inputPath, out string intermediateBitRatePath, BitRate? bitRate, 
            string outputDirectory, string outputFileName, bool isAlreadyConvertedToBitRateHigh)
        {
            if (!bitRate.HasValue)
            {
                intermediateBitRatePath = inputPath;
                return;
            }

            using (var reader = new AudioFileReader(inputPath))
            {
                var actualBitRate = BitRateHelper.GetBitRate(BitRate.Low);

                if (reader.WaveFormat.BitsPerSample == actualBitRate)
                {
                    intermediateBitRatePath = inputPath;
                    return;
                }

                var intermediateBitRateFileName = $"{outputFileName}-intermediate-bitrate.wav";
                intermediateBitRatePath = Path.Combine(outputDirectory, intermediateBitRateFileName);
                var originalFormat = reader.WaveFormat;

                if (bitRate.Value == BitRate.High)
                {
                    if (!isAlreadyConvertedToBitRateHigh)
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

        static void Convert(
            string inputPath, out string intermediateSampleRatePath, SampleRate? sampleRate, 
            string outputDirectory, string outputFileName, ref bool isAlreadyConvertedToBitRateHigh)
        {
            if (!sampleRate.HasValue)
            {
                intermediateSampleRatePath = inputPath;
                return;
            }

            var actualSampleRate = SampleRateHelper.GetSampleRate(sampleRate.Value);

            using (var reader = new AudioFileReader(inputPath))
            {
                if (reader.WaveFormat.SampleRate != actualSampleRate)
                {
                    var intermediateSampleRateFileName = $"{outputFileName}-intermediate-samplerate.wav";
                    intermediateSampleRatePath = Path.Combine(outputDirectory, intermediateSampleRateFileName);
                    var resampler = new WdlResamplingSampleProvider(reader, actualSampleRate);
                    WaveFileWriter.CreateWaveFile16(intermediateSampleRatePath, resampler);
                    isAlreadyConvertedToBitRateHigh = true;
                }
                else
                {
                    intermediateSampleRatePath = inputPath;
                }
            }
        }
    }
}
