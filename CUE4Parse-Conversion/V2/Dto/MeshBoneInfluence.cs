namespace CUE4Parse_Conversion.V2.Dto;

public readonly struct MeshBoneInfluence(ushort bone, ushort rawWeight, float weight)
{
    public readonly ushort Bone = bone;
    public readonly ushort RawWeight = rawWeight;
    public readonly float Weight = weight;
}
