using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Pak.Objects;

[StructLayout(LayoutKind.Sequential)]
public struct FPakCompressedBlock(long compressedStart, long compressedEnd)
{
    public long CompressedStart = compressedStart;
    public long CompressedEnd = compressedEnd;
    public readonly long Size => CompressedEnd - CompressedStart;

    public override readonly string ToString() => $"From {CompressedStart} To {CompressedEnd} (={Size})";
}
