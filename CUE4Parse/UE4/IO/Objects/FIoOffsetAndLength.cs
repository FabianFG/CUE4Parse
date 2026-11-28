using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.IO.Objects;

[StructLayout(LayoutKind.Sequential, Size = 10)]
public readonly struct FIoOffsetAndLength
{
    public readonly Bytes10 OffsetAndLength;

    public ulong Offset => OffsetAndLength[4]
                                | ((ulong) OffsetAndLength[3] << 8)
                                | ((ulong) OffsetAndLength[2] << 16)
                                | ((ulong) OffsetAndLength[1] << 24)
                                | ((ulong) OffsetAndLength[0] << 32);
    public ulong Length => OffsetAndLength[9]
                                | ((ulong) OffsetAndLength[8] << 8)
                                | ((ulong) OffsetAndLength[7] << 16)
                                | ((ulong) OffsetAndLength[6] << 24)
                                | ((ulong) OffsetAndLength[5] << 32);

    internal static FIoOffsetAndLength FromMisaOnline(ReadOnlySpan<byte> bytes)
    {
        Span<byte> reordered = stackalloc byte[10];
        reordered[0] = bytes[7];
        reordered[1] = bytes[2];
        reordered[2] = bytes[9];
        reordered[3] = bytes[8];
        reordered[4] = bytes[3];
        reordered[5] = bytes[0];
        reordered[6] = bytes[5];
        reordered[7] = bytes[1];
        reordered[8] = bytes[6];
        reordered[9] = bytes[4];
        return MemoryMarshal.Read<FIoOffsetAndLength>(reordered);
    }

    [InlineArray(10)]
    public struct Bytes10
    {
        private byte _element0;
    }

    public override string ToString()
    {
        return $"{nameof(Offset)} {Offset} | {nameof(Length)} {Length}";
    }
}
