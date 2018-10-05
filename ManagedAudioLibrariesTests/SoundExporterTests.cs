using System;
using System.IO;
using NAudio.Wave;
using SoundExporter;
using Xunit;

namespace ManagedAudioLibrariesTests
{
    public class SoundExporterTests : IDisposable
    {
        static readonly TimeSpan _expectedTotalTime;
        static readonly TimeSpan _totalTimeDelta = TimeSpan.FromMilliseconds(1);

        const string DefaultOutputPath = "Bar.wav";
        const string OutputPathFormat = "{0}.wav";

        static SoundExporterTests()
        {
            using (var reader = new AudioFileReader(AudioFiles.WavFilename))
            {
                _expectedTotalTime = reader.TotalTime;
            }
        }

        [Theory]
        [InlineData(SampleRate.Low, BitRate.Low, ChannelFormat.Mono)]
        [InlineData(SampleRate.Low, BitRate.Low, ChannelFormat.Stereo)]
        [InlineData(SampleRate.Low, BitRate.High, ChannelFormat.Mono)]
        [InlineData(SampleRate.Low, BitRate.High, ChannelFormat.Stereo)]
        [InlineData(SampleRate.High, BitRate.Low, ChannelFormat.Mono)]
        [InlineData(SampleRate.High, BitRate.Low, ChannelFormat.Stereo)]
        [InlineData(SampleRate.High, BitRate.High, ChannelFormat.Mono)]
        [InlineData(SampleRate.High, BitRate.High, ChannelFormat.Stereo)]
        public void ConvertTest(SampleRate sampleRate, BitRate bitRate, ChannelFormat channelFormat)
        {
            int actualSampleRate = SampleRateHelper.GetSampleRate(sampleRate);
            int actualBitRate = BitRateHelper.GetBitRate(bitRate);
            int actualChannelsCount = ChannelFormatHelper.GetChannelsCount(channelFormat);
            var fileName = $"{nameof(ConvertTest)}-SR{actualSampleRate}-BR{actualBitRate}-CF{actualChannelsCount}";
            var outputPath = string.Format(OutputPathFormat, fileName);
            var isSuccess = WavConverter.TryConvert(
                AudioFiles.WavFilename, outputPath, sampleRate, channelFormat, bitRate);

            Assert.True(isSuccess);

            using (var reader = new WaveFileReader(outputPath))
            {
                Assert.Equal(actualSampleRate, reader.WaveFormat.SampleRate);
                Assert.Equal(actualBitRate, reader.WaveFormat.BitsPerSample);
                Assert.Equal(actualChannelsCount, reader.WaveFormat.Channels);
                Assert.InRange(
                    _expectedTotalTime, reader.TotalTime - _totalTimeDelta, reader.TotalTime + _totalTimeDelta);
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

        public void Dispose()
        {
            DeleteIfExists(DefaultOutputPath);
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
