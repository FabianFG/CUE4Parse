using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FIntVector : IUStruct
{
    public readonly int X;
    public readonly int Y;
    public readonly int Z;

    public static FIntVector Zero => new FIntVector(0, 0, 0);

    public FIntVector(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public override string ToString() => $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Z)}: {Z}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FIntVector operator +(FIntVector a, FIntVector b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FIntVector operator +(FIntVector a, int bias) => new(a.X + bias, a.Y + bias, a.Z + bias);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector operator +(FIntVector a, FVector b) => new FVector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector operator +(FIntVector a, float bias) => new FVector(a.X + bias, a.Y + bias, a.Z + bias);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FIntVector operator -(FIntVector a, FIntVector b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FIntVector operator -(FIntVector a, int bias) => new(a.X - bias, a.Y - bias, a.Z - bias);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector operator -(FIntVector a, FVector b) => new FVector(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector operator -(FIntVector a, float bias) => new FVector(a.X - bias, a.Y - bias, a.Z - bias);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FIntVector operator *(FIntVector a, FIntVector b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FIntVector operator *(FIntVector a, int bias) => new(a.X * bias, a.Y * bias, a.Z * bias);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector operator *(FIntVector a, FVector b) => new FVector(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector operator *(FIntVector a, float bias) => new FVector(a.X * bias, a.Y * bias, a.Z * bias);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FIntVector operator /(FIntVector a, FIntVector b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FIntVector operator /(FIntVector a, int bias) => new(a.X / bias, a.Y / bias, a.Z / bias);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector operator /(FIntVector a, FVector b) => new FVector(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector operator /(FIntVector a, float bias) => new FVector(a.X / bias, a.Y / bias, a.Z / bias);

}
