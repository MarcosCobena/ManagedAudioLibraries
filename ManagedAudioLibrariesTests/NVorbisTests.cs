using NVorbis;
using Xunit;

namespace ManagedAudioLibrariesTests
{
    public class NVorbisTests
    {
        [Fact]
        public void OggDurationTest()
        {
            using (var vorbis = new VorbisReader(AudioFiles.OggFilename))
            {
                Assert.True(vorbis.TotalTime.TotalMilliseconds > 0);
            }
        }
    }
}
