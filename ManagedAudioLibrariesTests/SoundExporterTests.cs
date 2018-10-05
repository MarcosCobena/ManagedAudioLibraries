using System;
using System.IO;
using NAudio.Wave;
using SoundExporter;
using Xunit;

namespace ManagedAudioLibrariesTests
{
    public class SoundExporterTests : IDisposable
    {
        static readonly string[] _inputPaths =
        {
            AudioFiles.WavSR22050BR16CF1Filename,
            AudioFiles.WavSR22050BR16CF2Filename,
            AudioFiles.WavSR22050BR8CF1Filename,
            AudioFiles.WavSR22050BR8CF2Filename,
            AudioFiles.WavSR44100BR16CF1Filename,
            AudioFiles.WavSR44100BR16CF2Filename,
            AudioFiles.WavSR44100BR8CF1Filename,
            AudioFiles.WavSR44100BR8CF2Filename
        };
        static readonly TimeSpan _totalTimeDelta = TimeSpan.FromMilliseconds(10);

        const string DefaultOutputPath = "Bar.wav";
        const string OutputPathFormat = "{0}.wav";

        public static TheoryData<string, SampleRate, BitRate, ChannelFormat> ConversionTestData
        {
            get
            {
                var data = new TheoryData<string, SampleRate, BitRate, ChannelFormat>();

                foreach (var path in _inputPaths)
                {
                    data.Add(path, SampleRate.Low, BitRate.Low, ChannelFormat.Mono);
                    data.Add(path, SampleRate.Low, BitRate.Low, ChannelFormat.Stereo);
                    data.Add(path, SampleRate.Low, BitRate.High, ChannelFormat.Mono);
                    data.Add(path, SampleRate.Low, BitRate.High, ChannelFormat.Stereo);
                    data.Add(path, SampleRate.High, BitRate.Low, ChannelFormat.Mono);
                    data.Add(path, SampleRate.High, BitRate.Low, ChannelFormat.Stereo);
                    data.Add(path, SampleRate.High, BitRate.High, ChannelFormat.Mono);
                    data.Add(path, SampleRate.High, BitRate.High, ChannelFormat.Stereo);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(ConversionTestData))]
        public void ConversionTest(
            string inputPath, SampleRate sampleRate, BitRate bitRate, ChannelFormat channelFormat)
        {
            var inputFileName = Path.GetFileNameWithoutExtension(inputPath);
            var actualSampleRate = SampleRateHelper.GetSampleRate(sampleRate);
            var actualBitRate = BitRateHelper.GetBitRate(bitRate);
            var actualChannelsCount = ChannelFormatHelper.GetChannelsCount(channelFormat);
            var fileName = $"{inputFileName}-SR{actualSampleRate}-BR{actualBitRate}-CF{actualChannelsCount}";
            var outputPath = string.Format(OutputPathFormat, fileName);
            var isSuccess = WavConverter.TryConvert(inputPath, outputPath, sampleRate, channelFormat, bitRate);

            Assert.True(isSuccess);

            using (var inputReader = new AudioFileReader(inputPath))
            using (var outputReader = new WaveFileReader(outputPath))
            {
                Assert.Equal(actualSampleRate, outputReader.WaveFormat.SampleRate);
                Assert.Equal(actualBitRate, outputReader.WaveFormat.BitsPerSample);
                Assert.Equal(actualChannelsCount, outputReader.WaveFormat.Channels);
                Assert.InRange(
                    inputReader.TotalTime, outputReader.TotalTime - _totalTimeDelta, 
                    outputReader.TotalTime + _totalTimeDelta);
            }

#if !DEBUG
            DeleteIfExists(outputPath);
#endif
        }

        [Fact]
        public void UnexistingInputTest()
        {
            Assert.Throws<FileNotFoundException>(
                () => WavConverter.TryConvert("Foo.wav", DefaultOutputPath, SampleRate.Low));
        }

        [Fact]
        public void Conversionless()
        {
            var isSuccess = WavConverter.TryConvert(AudioFiles.WavFilename, DefaultOutputPath);
            var existsOutputPath = File.Exists(DefaultOutputPath);

            Assert.False(isSuccess);
            Assert.False(existsOutputPath);
        }

        [Fact]
        public void BitRate24ConversionTest()
        {
            ConversionTest(AudioFiles.WavSR44100BR24CF2Filename, SampleRate.Low, BitRate.Low, ChannelFormat.Mono);
        }

        [Fact]
        public void LargeFileConversionTest()
        {
            ConversionTest(AudioFiles.LargeWavSR44100BR16CF2Filename, SampleRate.Low, BitRate.Low, ChannelFormat.Mono);
        }

        public void Dispose()
        {
#if !DEBUG
            DeleteIfExists(DefaultOutputPath);
#endif
        }

        static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
