using System.IO;

namespace SoundExporter
{
    static class PCMConverter
    {
        public static unsafe MemoryStream ConvertPCM16ToPCM8(Stream reader)
        {
            var length = (int)(reader.Length - reader.Position);
            var outputStream = new MemoryStream(length / sizeof(short));
            var buffer = new byte[sizeof(short)];

            fixed (byte* bufferRef = &buffer[0])
            {
                var sampleCount = length / sizeof(short);
                var sample = (short*)bufferRef;

                for (int i = 0; i < sampleCount; i++)
                {
                    reader.Read(buffer, 0, buffer.Length);
                    var value = (byte)((*sample + short.MaxValue) >> 8);
                    outputStream.WriteByte(value);
                }
            }

            outputStream.Seek(0, SeekOrigin.Begin);

            return outputStream;
        }
    }
}
