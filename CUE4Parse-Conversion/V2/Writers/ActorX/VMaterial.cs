using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.V2.Writers.ActorX;

public readonly struct VMaterial
{
    public readonly string MaterialName;
    public readonly int TextureIndex;
    public readonly uint PolyFlags;
    public readonly int AuxMaterial;
    public readonly uint AuxFlags;
    public readonly int LodBias;
    public readonly int LodStyle;

    public VMaterial(string materialName, int textureIndex, uint polyFlags, int auxMaterial, uint auxFlags, int lodBias, int lodStyle)
    {
        MaterialName = materialName;
        TextureIndex = textureIndex;
        PolyFlags = polyFlags;
        AuxMaterial = auxMaterial;
        AuxFlags = auxFlags;
        LodBias = lodBias;
        LodStyle = lodStyle;
    }

    public void Serialize(FArchiveWriter Ar)
    {
        Ar.Write(MaterialName, 64);
        Ar.Write(TextureIndex);
        Ar.Write(PolyFlags);
        Ar.Write(AuxMaterial);
        Ar.Write(AuxFlags);
        Ar.Write(LodBias);
        Ar.Write(LodStyle);
    }
}
