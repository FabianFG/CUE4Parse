using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Core.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FUIntVector : IUStruct
{
    public readonly uint X;
    public readonly uint Y;
    public readonly uint Z;

    public static FUIntVector Zero => new FUIntVector(0, 0, 0);

    public FUIntVector(uint x, uint y, uint z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public FUIntVector(int x, int y, int z)
    {
        X = (uint)x;
        Y = (uint)y;
        Z = (uint)z;
    }

    public override string ToString() => $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Z)}: {Z}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FUIntVector operator +(FUIntVector a, FUIntVector b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FUIntVector operator +(FUIntVector a, uint bias) => new(a.X + bias, a.Y + bias, a.Z + bias);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector operator +(FUIntVector a, FVector b) => new FVector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector operator +(FUIntVector a, float bias) => new FVector(a.X + bias, a.Y + bias, a.Z + bias);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FUIntVector operator *(FUIntVector a, FUIntVector b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FUIntVector operator *(FUIntVector a, uint bias) => new(a.X * bias, a.Y * bias, a.Z * bias);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector operator *(FUIntVector a, FVector b) => new FVector(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector operator *(FUIntVector a, float bias) => new FVector(a.X * bias, a.Y * bias, a.Z * bias);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FUIntVector operator /(FUIntVector a, FUIntVector b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FUIntVector operator /(FUIntVector a, uint bias) => new(a.X / bias, a.Y / bias, a.Z / bias);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector operator /(FUIntVector a, FVector b) => new FVector(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector operator /(FUIntVector a, float bias) => new FVector(a.X / bias, a.Y / bias, a.Z / bias);
}
