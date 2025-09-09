using System;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FVector3UnsignedShort : IUStruct
{
    public readonly Half X;
    public readonly Half Y;
    public readonly Half Z;
    public readonly Half W;

    public FVector3UnsignedShort(Half x, Half y, Half z, Half w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    // not sure about W value, seems to be always 0x00 0x3c
    public static implicit operator FVector(FVector3UnsignedShort v) => new((float)v.X, (float)(v.Y), (float)(v.Z));
}
