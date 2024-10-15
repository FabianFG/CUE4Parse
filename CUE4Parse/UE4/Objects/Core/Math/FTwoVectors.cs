using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Core.Math;

[StructLayout(LayoutKind.Sequential)]
public struct FTwoVectors : IUStruct
{
    public FVector V1;
    public FVector V2;

    public FTwoVectors(FVector v1, FVector v2)
    {
        V1 = v1;
        V2 = v2;
    }

    public FTwoVectors(FArchive Ar)
    {
        V1 = new FVector(Ar);
        V2 = new FVector(Ar);
    }

    public override string ToString() => $"V1: {V1}, V2: {V2}";
}
