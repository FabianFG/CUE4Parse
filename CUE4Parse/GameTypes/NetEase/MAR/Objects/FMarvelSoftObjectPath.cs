using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.NetEase.MAR.Objects;

public struct FMarvelSoftObjectPath(FAssetArchive Ar) : IUStruct
{
    public FSoftObjectPath SoftObjectPath = new( Ar.ReadFString(), "", Ar.Owner);
}
