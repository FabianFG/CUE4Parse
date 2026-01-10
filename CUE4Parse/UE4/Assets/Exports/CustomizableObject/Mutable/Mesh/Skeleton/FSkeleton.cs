using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Skeleton;

public class FSkeleton
{
    public FBoneName[] BoneIds;
    public short[] BoneParents;

    public FSkeleton(FMutableArchive Ar)
    {
        BoneIds = Ar.ReadArray<FBoneName>();
        BoneParents = Ar.ReadArray<short>();
    }
}
