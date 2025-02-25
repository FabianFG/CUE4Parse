using System.Runtime.InteropServices;
using static CUE4Parse.Utils.TypeConversionUtils;

namespace CUE4Parse.UE4.Objects.Core.Math;

[StructLayout(LayoutKind.Sequential)]
public struct FHalfVector(ushort x, ushort y, ushort z)
{
    public ushort X = x;
    public ushort Y = y;
    public ushort Z = z;

    public static implicit operator FVector(FHalfVector h) => new(HalfToFloat(h.X), HalfToFloat(h.Y), HalfToFloat(h.Z));
}

[StructLayout(LayoutKind.Sequential)]
public struct FHalfVector4(ushort x, ushort y, ushort z, ushort w)
{
    public ushort X = x;
    public ushort Y = y;
    public ushort Z = z;
    public ushort W = w;

    public static implicit operator FVector(FHalfVector4 h) => new FVector(HalfToFloat(h.X), HalfToFloat(h.Y), HalfToFloat(h.Z)) * HalfToFloat(h.W);
}
