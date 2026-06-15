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
