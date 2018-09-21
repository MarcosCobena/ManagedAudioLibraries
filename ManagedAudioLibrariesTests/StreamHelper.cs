using System.IO;

namespace ManagedAudioLibrariesTests
{
    public static class StreamHelper
    {
        public static byte[] ToArray(this Stream stream)
        {
            byte[] bytes;

            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }

            return bytes;
        }
    }
}
