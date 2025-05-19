using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Engine.Curves;

public struct FNameCurveKey(FAssetArchive Ar) : IUStruct
{
    public float Time = Ar.Read<float>();
    public FName Value = Ar.ReadFName();
}
