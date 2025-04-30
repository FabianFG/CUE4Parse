using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FVector3SignedShortScale : IUStruct
{
    public readonly short X;
    public readonly short Y;
    public readonly short Z;
    public readonly short W;

    public FVector3SignedShortScale(short x, short y, short z, short w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public static implicit operator FVector(FVector3SignedShortScale v)
    {
        // W having the value of short.MaxValue makes me believe I should use it (somehow) instead of a hardcoded constant
        float wf = v.W == 0 ? 1f : v.W;
        return new(v.X / wf, v.Y / wf, v.Z / wf);
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FVector3UnsignedShortScale : IUStruct
{
    public readonly ushort X;
    public readonly ushort Y;
    public readonly ushort Z;
    public readonly ushort W;

    public FVector3UnsignedShortScale(ushort x, ushort y, ushort z, ushort w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public static implicit operator FVector(FVector3UnsignedShortScale v) => new(v.X, v.Y, v.Z);
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FVector3Packed32 : IUStruct
{
    public readonly uint Data;
    public float X => Data & 0x3ff;
    public float Y => (Data >> 10) & 0x3ff;
    public float Z => (Data >> 20) & 0x3ff;

    public static implicit operator FVector(FVector3Packed32 v) => new(v.X, v.Y, v.Z);
}
