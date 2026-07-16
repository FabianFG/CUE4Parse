using System.Runtime.InteropServices;

namespace CUE4Parse.GameTypes.HonorOfKings.Vfs.Objects;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FHoKCompressedChunk
{
    public readonly long Offset;
    public readonly int CompressedSize;
    public readonly int UncompressedSize;
    public readonly int Padding;
    public readonly int Index;

    public FHoKCompressedChunk(long offset, int compessedSize, int uncompressedSize, int padding,  int index)
    {
        Offset = offset;
        CompressedSize = compessedSize;
        UncompressedSize = uncompressedSize;
        Padding = padding;
        Index = index;
    }
}

