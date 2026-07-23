using CUE4Parse.UE4.Assets.Exports.Nanite;

namespace CUE4Parse_Conversion.Dto;

public readonly struct MeshBoneInfluenceDto(ushort bone, ushort rawWeight, float weight)
{
    public readonly ushort Bone = bone;
    public readonly ushort RawWeight = rawWeight;
    public readonly float Weight = weight;

    public MeshBoneInfluenceDto(FBoneInfluence influence) : this((ushort)influence.BoneIndex, (ushort)(influence.BoneWeight * ushort.MaxValue), influence.BoneWeight)
    {

    }
}
