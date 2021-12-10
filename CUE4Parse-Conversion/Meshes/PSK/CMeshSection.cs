using CUE4Parse.UE4.Assets;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CMeshSection
    {
        public string? MaterialName;
        public ResolvedObject? Material; // UMaterialInterface
        public int FirstIndex;
        public int NumFaces;

        public CMeshSection(string? materialName, ResolvedObject? material, int firstIndex, int numFaces)
        {
            MaterialName = materialName;
            Material = material;
            FirstIndex = firstIndex;
            NumFaces = numFaces;
        }
    }
}
