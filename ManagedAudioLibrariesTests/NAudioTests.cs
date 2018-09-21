using System.IO;
using NAudio.Vorbis;
using NAudio.Wave;
using Xunit;

namespace ManagedAudioLibrariesTests
{
    public class NAudioTests
    {
        [Fact]
        public void Mp3ToPcmConversionTest()
        {
            var bytes = Convert(new Mp3FileReader(AudioFiles.Mp3Filename));

            AssertExtensions.AnyNotZero(bytes);
        }

        [Fact]
        public void OggToPcmConversionTest()
        {
            var bytes = Convert(new VorbisWaveReader(AudioFiles.OggFilename));

            AssertExtensions.AnyNotZero(bytes);
        }

        [Fact]
        public void WavToPcmConversionTest()
        {
            var bytes = Convert(new WaveFileReader(AudioFiles.WavFilename));

            AssertExtensions.AnyNotZero(bytes);
        }

        private static byte[] Convert(WaveStream reader)
        {
            byte[] bytes;

            using (var converter = WaveFormatConversionStream.CreatePcmStream(reader))
            using (var outputStream = new MemoryStream())
            using (var writer = new WaveFileWriter(outputStream, converter.WaveFormat))
            {
                bytes = new byte[converter.Length];
                converter.Position = 0;
                converter.Read(bytes, 0, (int)converter.Length);
                writer.Write(bytes, 0, bytes.Length);
                writer.Flush();
            }

            return bytes;
        }
    }
}
