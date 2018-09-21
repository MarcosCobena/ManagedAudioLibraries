using NLayer;
using Xunit;

namespace ManagedAudioLibrariesTests
{
    public class NLayerTests
    {
        [Fact]
        public void Mp3ToPcmConversionTest()
        {
            var mpegFile = new MpegFile(AudioFiles.Mp3Filename);
            var samples = new float[mpegFile.SampleRate];
            mpegFile.ReadSamples(samples, 0, mpegFile.SampleRate);

            AssertExtensions.AnyNotZero(samples);
            Assert.True(mpegFile.Duration.TotalMilliseconds > 0);
        }
    }
}
