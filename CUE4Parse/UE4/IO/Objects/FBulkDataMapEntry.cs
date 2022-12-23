using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.IO.Objects
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct FBulkDataMapEntry
    {
        public const uint Size = 32;

        public readonly ulong SerialOffset;
        public readonly ulong DuplicateSerialOffset;
        public readonly ulong SerialSize;
        public readonly uint Flags;
        public readonly uint Pad;
    }
}