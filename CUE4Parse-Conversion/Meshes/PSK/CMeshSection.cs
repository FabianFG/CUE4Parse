using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CMeshSection
    {
        public string? MaterialName;
        public FPackageIndex? Material; // UMaterialInterface
        public int FirstIndex;
        public int NumFaces;

        public CMeshSection(string? materialName, FPackageIndex? material, int firstIndex, int numFaces)
        {
            MaterialName = materialName;
            Material = material;
            FirstIndex = firstIndex;
            NumFaces = numFaces;
        }
    }
}
