using System.Runtime.InteropServices;
using static CUE4Parse.Utils.TypeConversionUtils;

namespace CUE4Parse.UE4.Objects.Core.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FVector3UnsignedShort(ushort x, ushort y, ushort z, ushort w) : IUStruct
{
    public readonly ushort X = x;
    public readonly ushort Y = y;
    public readonly ushort Z = z;
    public readonly ushort W = w;

    // not sure about W value, seems to be always 0x00 0x3c
    public static implicit operator FVector(FVector3UnsignedShort v) => new(HalfToFloat(v.X), HalfToFloat(v.Y), HalfToFloat(v.Z));
}
