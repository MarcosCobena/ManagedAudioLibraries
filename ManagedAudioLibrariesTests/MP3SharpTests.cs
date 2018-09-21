using MP3Sharp;
using Xunit;

namespace ManagedAudioLibrariesTests
{
    public class MP3SharpTests
    {
        [Fact]
        public void Mp3ToPcmConversionTest()
        {
            using (var mp3Stream = new MP3Stream(AudioFiles.Mp3Filename))
            {
                var bytes = mp3Stream.ToArray();
                AssertExtensions.AnyNotZero(bytes);
            }
        }
    }
}
