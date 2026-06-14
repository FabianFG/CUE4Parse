namespace CUE4Parse_Conversion.Dto;

public readonly struct MeshBoneInfluenceDto(ushort bone, ushort rawWeight, float weight)
{
    public readonly ushort Bone = bone;
    public readonly ushort RawWeight = rawWeight;
    public readonly float Weight = weight;
}
