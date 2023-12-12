using System.Runtime.InteropServices;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Objects.Core.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FIntVector : IUStruct, ISerializable
{
    public readonly int X;
    public readonly int Y;
    public readonly int Z;

    public FIntVector(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(X);
        Ar.Write(Y);
        Ar.Write(Z);
    }

    public override string ToString() => $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Z)}: {Z}";
}