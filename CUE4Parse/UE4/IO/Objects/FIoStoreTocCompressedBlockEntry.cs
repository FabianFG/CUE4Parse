using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects;

[StructLayout(LayoutKind.Sequential, Size = 12)]
public readonly struct FIoStoreTocCompressedBlockEntry
{
    public const int SIZE = 5 + 3 + 3 + 1;

    private const int OffsetBits = 40;
    private const ulong OffsetMask = (1ul << OffsetBits) - 1ul;
    private const int SizeBits = 24;
    private const uint SizeMask = (1 << SizeBits) - 1;
    private const int SizeShift = 8;

    public long Offset => (long) (Offset_CompressedSize & OffsetMask);
    public uint CompressedSize => (uint) ((Offset_CompressedSize >> OffsetBits) & SizeMask);
    public uint UncompressedSize => UncompressedSize_CompressionMethodIndex & SizeMask;
    public byte CompressionMethodIndex => (byte) (UncompressedSize_CompressionMethodIndex >> SizeBits);
    public readonly ulong Offset_CompressedSize;
    public readonly uint UncompressedSize_CompressionMethodIndex;


    public FIoStoreTocCompressedBlockEntry(FArchive Ar)
    {
        Offset_CompressedSize = Ar.Read<ulong>();
        UncompressedSize_CompressionMethodIndex = Ar.Read<uint>();
    }

    public override string ToString()
    {
        return $"{nameof(Offset)} {Offset}: From {CompressedSize} To {UncompressedSize}";
    }
}
