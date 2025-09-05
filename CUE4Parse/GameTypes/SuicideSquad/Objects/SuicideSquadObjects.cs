using System.Runtime.InteropServices;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.GameTypes.SuicideSquad.Objects;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct FVectorShort
{
    public ushort X;
    public ushort Y;
    public ushort Z;

    public static implicit operator FVector(FVectorShort vector) => new FVector(vector.X, vector.Y, vector.Z);
}

[StructLayout(LayoutKind.Sequential)]
public struct FSimpleBox : IUStruct
{
    public FVector A;
    public FVector B;
}

[StructLayout(LayoutKind.Sequential)]
public struct FRRotationTranslation : IUStruct
{
    public FQuat Rotation;
    public FVector Translation;
}
