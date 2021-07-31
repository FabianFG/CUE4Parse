using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CMeshSection
    {
        public FPackageIndex? Material; // UMaterialInterface
        public int FirstIndex;
        public int NumFaces;

        public CMeshSection(FPackageIndex? material, int firstIndex, int numFaces)
        {
            Material = material;
            FirstIndex = firstIndex;
            NumFaces = numFaces;
        }
    }
}