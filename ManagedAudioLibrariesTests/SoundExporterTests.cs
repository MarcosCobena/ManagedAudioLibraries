using System;
using System.Runtime.CompilerServices;
using NAudio.Wave;
using SoundExporter;
using Xunit;

namespace ManagedAudioLibrariesTests
{
    public class SoundExporterTests
    {
        static readonly TimeSpan _expectedTotalTime;
        static readonly TimeSpan _totalTimeDelta = TimeSpan.FromMilliseconds(1);

        const string OutputPathFormat = "{0}.wav";
        const int ExpectedBitRateLow = 8;
        const int ExpectedBitRateHigh = 16;
        const int ExpectedChannelsMonoCount = 1;
        const int ExpectedChannelsStereoCount = 2;
        const int ExpectedSampleRateLow = 22050;
        const int ExpectedSampleRateHigh = 44100;

        static SoundExporterTests()
        {
            using (var reader = new AudioFileReader(AudioFiles.WavFilename))
            {
                _expectedTotalTime = reader.TotalTime;
            }
        }

        // TODO refactor with theories
        [Fact]
        public void SampleRateLowBitRateLowChannelFormatMonoTest()
        {
            ConvertAndAssert(
                SampleRate.Low, BitRate.Low, ChannelFormat.Mono, ExpectedSampleRateLow, ExpectedBitRateLow, 
                ExpectedChannelsMonoCount);
        }

        [Fact]
        public void SampleRateLowBitRateLowChannelFormatStereoTest()
        {
            ConvertAndAssert(
                SampleRate.Low, BitRate.Low, ChannelFormat.Stereo, ExpectedSampleRateLow, ExpectedBitRateLow, 
                ExpectedChannelsStereoCount);
        }

        [Fact]
        public void SampleRateLowBitRateHighChannelFormatMonoTest()
        {
            ConvertAndAssert(
                SampleRate.Low, BitRate.High, ChannelFormat.Mono, ExpectedSampleRateLow, ExpectedBitRateHigh, 
                ExpectedChannelsMonoCount);
        }

        [Fact]
        public void SampleRateLowBitRateHighChannelFormatStereoTest()
        {
            ConvertAndAssert(
                SampleRate.Low, BitRate.High, ChannelFormat.Stereo, ExpectedSampleRateLow, ExpectedBitRateHigh, 
                ExpectedChannelsStereoCount);
        }

        [Fact]
        public void SampleRateHighBitRateLowChannelFormatMonoTest()
        {
            ConvertAndAssert(
                SampleRate.High, BitRate.Low, ChannelFormat.Mono, ExpectedSampleRateHigh, ExpectedBitRateLow, 
                ExpectedChannelsMonoCount);
        }

        [Fact]
        public void SampleRateHighBitRateLowChannelFormatStereoTest()
        {
            ConvertAndAssert(
                SampleRate.High, BitRate.Low, ChannelFormat.Stereo, ExpectedSampleRateHigh, ExpectedBitRateLow, 
                ExpectedChannelsStereoCount);
        }

        [Fact]
        public void SampleRateHighBitRateHighChannelFormatMonoTest()
        {
            ConvertAndAssert(
                SampleRate.High, BitRate.High, ChannelFormat.Mono, ExpectedSampleRateHigh, ExpectedBitRateHigh, 
                ExpectedChannelsMonoCount);
        }

        [Fact]
        public void SampleRateHighBitRateHighChannelFormatStereoTest()
        {
            ConvertAndAssert(
                SampleRate.High, BitRate.High, ChannelFormat.Stereo, ExpectedSampleRateHigh, ExpectedBitRateHigh, 
                ExpectedChannelsStereoCount);
        }

        static void ConvertAndAssert(SampleRate sampleRate, BitRate bitRate, ChannelFormat channelFormat, 
                                     int expectedSampleRate, int expectedBitRate, int expectedChannelsCount, 
                                     [CallerMemberName] string memberName = "")
        {
            var outputPath = string.Format(OutputPathFormat, memberName);
            var isSuccess = WavConverter.TryConvert(
                AudioFiles.WavFilename, outputPath, sampleRate, channelFormat, bitRate);

            Assert.True(isSuccess);

            using (var reader = new WaveFileReader(outputPath))
            {
                Assert.Equal(expectedSampleRate, reader.WaveFormat.SampleRate);
                Assert.Equal(expectedBitRate, reader.WaveFormat.BitsPerSample);
                Assert.Equal(expectedChannelsCount, reader.WaveFormat.Channels);
                Assert.InRange(
                    _expectedTotalTime, reader.TotalTime - _totalTimeDelta, reader.TotalTime + _totalTimeDelta);
            }
        }
    }
}
