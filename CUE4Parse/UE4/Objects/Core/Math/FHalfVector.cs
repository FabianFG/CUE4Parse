using System;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FHalfVector
{
    public readonly Half X;
    public readonly Half Y;
    public readonly Half Z;

    public FHalfVector(Half x, Half y, Half z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static implicit operator FVector(FHalfVector h) => new((float)h.X, (float)h.Y, (float)h.Z);
    public static implicit operator FHalfVectorScaled(FHalfVector v) => new(v.X, v.Y, v.Z, Half.Zero);
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public readonly struct FHalfVector4
{
    public readonly Half X;
    public readonly Half Y;
    public readonly Half Z;
    public readonly Half W;

    public FHalfVector4(Half x, Half y, Half z, Half w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public static implicit operator FVector(FHalfVector4 h) => new FVector((float)h.X, (float)h.Y, (float)h.Z) * (float)h.W;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public readonly struct FHalfVectorScaled
{
    public readonly Half X;
    public readonly Half Y;
    public readonly Half Z;
    public readonly Half W;

    public FHalfVectorScaled(Half x, Half y, Half z, Half w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public static implicit operator FVector(FHalfVectorScaled h)
    {
        float wf = h.W == Half.Zero ? 1.0f : (float)h.W;
        return new FVector((float) h.X, (float) h.Y, (float) h.Z) * wf;
    }
}
