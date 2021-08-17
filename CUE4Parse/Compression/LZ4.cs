#if USE_LZ4_NATIVE_LIB
using System.Runtime.InteropServices;

namespace CUE4Parse.Compression
{
    public static class LZ4
    {
        private const string LZ4_LIB_NAME = "msys-lz4-1";

        [DllImport(LZ4_LIB_NAME)]
        public static extern unsafe int LZ4_decompress_safe(byte* src, byte* dst, int compressedSize, int dstCapacity);

        [DllImport(LZ4_LIB_NAME)]
        public static extern int LZ4_compressBound(int inputSize);

        [DllImport(LZ4_LIB_NAME)]
        public static extern unsafe int LZ4_decompress_safe_partial(byte* src, byte* dst, int srcSize, int targetOutputSize, int dstCapacity);
    }
}
#endif