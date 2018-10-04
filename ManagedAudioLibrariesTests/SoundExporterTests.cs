using System.Runtime.CompilerServices;
using NAudio.Wave;
using SoundExporter;
using Xunit;

namespace ManagedAudioLibrariesTests
{
    public class SoundExporterTests
    {
        const string OutputPathFormat = "{0}.wav";

        [Fact]
        public void SampleRateLowTest()
        {
            ConvertAndAssertSampleRate(SampleRate.Low, 22050);
        }

        [Fact]
        public void SampleRateHighTest()
        {
            ConvertAndAssertSampleRate(SampleRate.High, 44100);
        }

        [Fact]
        public void BitRateLowTest()
        {
            ConvertAndAssertBitRate(BitRate.Low, 8);
        }

        [Fact]
        public void BitRateHighTest()
        {
            ConvertAndAssertBitRate(BitRate.High, 16);
        }

        [Fact]
        public void ChannelFormatMono()
        {
            ConvertAndAssertChannelFormat(ChannelFormat.Mono, 1);
        }

        [Fact]
        public void ChannelFormatStereo()
        {
            ConvertAndAssertChannelFormat(ChannelFormat.Stereo, 2);
        }

        static void ConvertAndAssertChannelFormat(
            ChannelFormat channelFormat, int expectedChannelsCount, [CallerMemberName] string memberName = "")
        {
            var outputPath = string.Format(OutputPathFormat, memberName);
            var isSuccess = WavConverter.TryConvert(AudioFiles.WavFilename, outputPath, channelFormat: channelFormat);

            Assert.True(isSuccess);

            using (var reader = new WaveFileReader(outputPath))
            {
                Assert.Equal(16, reader.WaveFormat.BitsPerSample);
                Assert.Equal(expectedChannelsCount, reader.WaveFormat.Channels);
            }
        }

        static void ConvertAndAssertBitRate(
            BitRate bitRate, int expectedBitRate, [CallerMemberName] string memberName = "")
        {
            var outputPath = string.Format(OutputPathFormat, memberName);
            var isSuccess = WavConverter.TryConvert(AudioFiles.WavFilename, outputPath, bitRate: bitRate);

            Assert.True(isSuccess);

            using (var reader = new WaveFileReader(outputPath))
            {
                Assert.Equal(expectedBitRate, reader.WaveFormat.BitsPerSample);
            }
        }

        static void ConvertAndAssertSampleRate(
            SampleRate sampleRate, int expectedSampleRate, [CallerMemberName] string memberName = "")
        {
            var outputPath = string.Format(OutputPathFormat, memberName);
            var isSuccess = WavConverter.TryConvert(AudioFiles.WavFilename, outputPath, sampleRate);

            Assert.True(isSuccess);

            using (var reader = new WaveFileReader(outputPath))
            {
                Assert.Equal(16, reader.WaveFormat.BitsPerSample);
                Assert.Equal(expectedSampleRate, reader.WaveFormat.SampleRate);
            }
        }
    }
}
