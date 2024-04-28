namespace CUE4Parse_Conversion.Meshes.PSK;

public record BoneInfluence(short Bone, byte RawWeight)
{
    public float Weight => RawWeight / 255.0f;
}
