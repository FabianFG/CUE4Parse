using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Skeleton;

[StructLayout(LayoutKind.Sequential)]
public struct FBonePose
{
    [JsonIgnore] public int Version = 2;
    public FBoneName BoneId;
    public EBoneUsageFlags BoneUsageFlags;
    public FTransform BoneTransform;
    public string? DeprecatedBoneName;

    public FBonePose(FMutableArchive Ar)
    {
        if (Ar.Game < EGame.GAME_UE5_5) Version = Ar.Read<int>();

        if (Version <= 1)
        {
            DeprecatedBoneName = Ar.ReadString();
        }
        else
        {
            BoneId = Ar.Read<FBoneName>();
        }

        if (Version == 0)
        {
            var skinned = Ar.Read<byte>();
            BoneUsageFlags = skinned != 0 ? EBoneUsageFlags.Skinning : EBoneUsageFlags.None;
        }
        else
        {
            BoneUsageFlags = Ar.Read<EBoneUsageFlags>();
        }

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
