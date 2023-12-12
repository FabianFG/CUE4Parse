using System.Runtime.InteropServices;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse.UE4.Objects.Core.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FIntPoint : IUStruct, ISerializable
{
    public readonly uint X;
    public readonly uint Y;

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(X);
        Ar.Write(Y);
    }

    public override string ToString() => $"{nameof(X)}: {X}, {nameof(Y)}: {Y}";
}