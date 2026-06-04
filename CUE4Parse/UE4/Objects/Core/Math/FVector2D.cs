using System.Numerics;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Core.Math;

/// <summary>
/// USE Ar.Read<FVector2D> FOR FLOATS AND new FVector2D(Ar) FOR DOUBLES
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct FVector2D : IUStruct,
    IMultiplyOperators<FVector2D,FVector2D,FVector2D>,
    IMultiplyOperators<FVector2D,float,FVector2D>,
    ISubtractionOperators<FVector2D,FVector2D,FVector2D>,
    ISubtractionOperators<FVector2D,float,FVector2D>,
    IAdditionOperators<FVector2D,FVector2D,FVector2D>,
    IAdditionOperators<FVector2D,float,FVector2D>
{
    public static readonly FVector2D ZeroVector = new(0, 0);

    public readonly float X;
    public readonly float Y;

    public FVector2D(float x, float y)
    {
        X = x;
        Y = y;
    }

    public FVector2D(FArchive Ar)
    {
        X = Ar.ReadFReal();
        Y = Ar.ReadFReal();
    }

    public static FVector2D operator +(FVector2D a, FVector2D b) => new(a.X + b.X, a.Y + b.Y);
    public static FVector2D operator -(FVector2D a, FVector2D b) => new(a.X - b.X, a.Y - b.Y);
    public static FVector2D operator *(FVector2D a, FVector2D b) => new(a.X * b.X, a.Y * b.Y);
    public static FVector2D operator /(FVector2D a, FVector2D b) => new(a.X / b.X, a.Y / b.Y);

    public static FVector2D operator +(FVector2D a, float b) => new(a.X + b, a.Y + b);
    public static FVector2D operator *(FVector2D a, float b) => new(a.X * b, a.Y * b);
    public static FVector2D operator -(FVector2D a, float b) => new(a.X - b, a.Y - b);

    public override string ToString() => $"X={X,3:F3} Y={Y,3:F3}";

    public static implicit operator Vector2(FVector2D v) => new(v.X, v.Y);
}

[StructLayout(LayoutKind.Sequential)]
public struct FVector2d : IUStruct,
    IMultiplyOperators<FVector2d, FVector2d, FVector2d>,
    IMultiplyOperators<FVector2d, float, FVector2d>,
    ISubtractionOperators<FVector2d, FVector2d, FVector2d>,
    ISubtractionOperators<FVector2d, float, FVector2d>,
    IAdditionOperators<FVector2d, FVector2d, FVector2d>,
    IAdditionOperators<FVector2d, float, FVector2d>
{
    public static readonly FVector2d ZeroVector = new(0, 0);

    public readonly double X;
    public readonly double Y;

    public FVector2d(float x, float y)
    {
        X = x;
        Y = y;
    }

    public FVector2d(double x, double y)
    {
        X = x;
        Y = y;
    }

    public FVector2d(FArchive Ar)
    {
        X = Ar.ReadFReal();
        Y = Ar.ReadFReal();
    }

    public static FVector2d operator +(FVector2d a, FVector2d b) => new(a.X + b.X, a.Y + b.Y);
    public static FVector2d operator -(FVector2d a, FVector2d b) => new(a.X - b.X, a.Y - b.Y);
    public static FVector2d operator *(FVector2d a, FVector2d b) => new(a.X * b.X, a.Y * b.Y);
    public static FVector2d operator /(FVector2d a, FVector2d b) => new(a.X / b.X, a.Y / b.Y);

    public static FVector2d operator +(FVector2d a, float b) => new(a.X + b, a.Y + b);
    public static FVector2d operator *(FVector2d a, float b) => new(a.X * b, a.Y * b);
    public static FVector2d operator -(FVector2d a, float b) => new(a.X - b, a.Y - b);

    public override string ToString() => $"X={X,3:F3} Y={Y,3:F3}";

    public static implicit operator FVector2D(FVector2d v) => new((float)v.X, (float)v.Y);
}
