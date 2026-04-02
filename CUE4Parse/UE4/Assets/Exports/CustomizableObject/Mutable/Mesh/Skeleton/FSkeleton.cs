using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Skeleton;

public class FSkeleton
{
    [JsonIgnore] public int Version = 7;
    public FBoneName[] BoneIds;
    public short[] BoneParents;

    public short[] BoneIds_DEPRECATED = [];
    public string[] OldBoneNames = [];
    public FTransform[] BoneTransforms_DEPRECATED = [];
    public bool bBoneTransformModified;

    public FSkeleton(FMutableArchive Ar)
    {
        if (Ar.Game < Versions.EGame.GAME_UE5_6) Version = Ar.Read<int>();

        if (Version >= 7)
        {
            BoneIds = Ar.ReadArray<FBoneName>();
        }
        else if (Version == 6)
        {
            BoneIds_DEPRECATED = Ar.ReadArray<short>();
        }
        else
        {
            OldBoneNames = Ar.ReadArray(Ar.ReadString);
        }

        if (Version == 3)
        {
            BoneTransforms_DEPRECATED = Ar.ReadArray<FTransform>();
        }

        BoneParents = Ar.ReadArray<short>();

        if (Version <= 4)
        {
            BoneIds_DEPRECATED = Ar.ReadArray<short>();
        }

        if (Version == 3)
        {
            bBoneTransformModified = Ar.ReadBoolean();
        }
    }
}
