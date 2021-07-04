using System;
using CUE4Parse.UE4.Assets.Exports.Material;

namespace CUE4Parse.UE4.Objects.Meshes
{
    public class CMeshSection
    {
        public Lazy<UMaterialInterface?>? Material;
        public int FirstIndex;
        public int NumFaces;

        public CMeshSection(Lazy<UMaterialInterface?>? material, int firstIndex, int numFaces)
        {
            Material = material;
            FirstIndex = firstIndex;
            NumFaces = numFaces;
        }
    }
}