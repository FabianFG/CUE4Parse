using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.Engine.Curves;

public struct FStringCurveKey(FAssetArchive Ar) : IUStruct
{
    public float Time = Ar.Read<float>();
    public string Value = Ar.ReadFString();
}
