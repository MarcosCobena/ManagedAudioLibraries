using System.Linq;
using Xunit;

namespace ManagedAudioLibrariesTests
{
    public static class AssertExtensions
    {
        public static void AnyNotZero(byte[] bytes)
        {
            var anyByteNotZero = bytes.Any(eachByte => eachByte != 0);
            Assert.True(anyByteNotZero, "Every byte is zero");
        }

        public static void AnyNotZero(float[] floats)
        {
            var anyFloatNotZero = floats.Any(eachFloat => eachFloat.CompareTo(0) == 0);
            Assert.True(anyFloatNotZero, "Every float is zero");
        }
    }
}
