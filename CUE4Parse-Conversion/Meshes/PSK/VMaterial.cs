using System;
using CUE4Parse.UE4.Writers;
using CUE4Parse_Conversion.ActorX;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class VMaterial
    {
        public readonly string MaterialName;
        public readonly int TextureIndex;
        public readonly uint PolyFlags;
        public readonly int AuxMaterial;
        public readonly uint AuxFlags;
        public readonly int LodBias;
        public readonly int LodStyle;

        public VMaterial(string materialName, int textureIndex, uint polyFlags,
            int auxMaterial, uint auxFlags, int lodBias, int lodStyle)
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
            Ar.Write(MaterialName.Substring(0, Math.Min(MaterialName.Length, 64)), 64);
            Ar.Write(TextureIndex);
            Ar.Write(PolyFlags);
            Ar.Write(AuxMaterial);
            Ar.Write(AuxFlags);
            Ar.Write(LodBias);
            Ar.Write(LodStyle);
        }
    }
}