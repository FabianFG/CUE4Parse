using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Pak.Objects;

[StructLayout(LayoutKind.Sequential)]
public struct FPakCompressedBlock
{
    public long CompressedStart;
    public long CompressedEnd;
    public long Size => CompressedEnd - CompressedStart;

    public FPakCompressedBlock(long compressedStart, long compressedEnd)
    {
        CompressedStart = compressedStart;
        CompressedEnd = compressedEnd;
    }

    public override string ToString() => $"From {CompressedStart} To {CompressedEnd} (={Size})";
}
