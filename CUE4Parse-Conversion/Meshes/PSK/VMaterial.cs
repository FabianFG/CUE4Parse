using System;
using System.Text;
using CUE4Parse.UE4.Writers;

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

        public void Serialize(FArchiveWriter writer)
        {
            var materialName = new byte[64];
            var material = Encoding.UTF8.GetBytes(MaterialName);
            Buffer.BlockCopy(material, 0, materialName, 0, material.Length);
            
            writer.Write(materialName);
            writer.Write(TextureIndex);
            writer.Write(PolyFlags);
            writer.Write(AuxMaterial);
            writer.Write(AuxFlags);
            writer.Write(LodBias);
            writer.Write(LodStyle);
        }
    }
}