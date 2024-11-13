using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Skeleton;

public class FBonePose
{
    public FBoneName BoneId;
    public EBoneUsageFlags BoneUsageFlags;
    public FTransform BoneTransform;

    public FBonePose(FArchive Ar)
    {
        BoneId = new FBoneName(Ar);
        BoneUsageFlags = Ar.Read<EBoneUsageFlags>();
        BoneTransform = Ar.Read<FTransform>();
    }
}

public enum EBoneUsageFlags : uint
{
    None		   = 0,
    Root		   = 1 << 1,
    Skinning	   = 1 << 2,
    SkinningParent = 1 << 3,
    Physics	       = 1 << 4,
    PhysicsParent  = 1 << 5,
    Deform         = 1 << 6,
    DeformParent   = 1 << 7,
    Reshaped       = 1 << 8
}
