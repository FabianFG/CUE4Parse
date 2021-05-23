using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Oodle.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FOodleCompressedData
    {
        public readonly uint Offset;
        public readonly uint CompressedLength;
        public readonly uint DecompressedLength;
    }
}