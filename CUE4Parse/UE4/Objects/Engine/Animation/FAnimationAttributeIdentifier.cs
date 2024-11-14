using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Engine.Animation;

public struct FAnimationAttributeIdentifier(FAssetArchive Ar) : IUStruct
{
    public FName Name = Ar.ReadFName();
    public FName BoneName = Ar.ReadFName();
    public int BoneIndex = Ar.Read<int>();
    public FSoftObjectPath ScriptStructPath = new FSoftObjectPath(Ar);
}
