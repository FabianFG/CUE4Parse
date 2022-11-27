using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;

namespace CUE4Parse_Conversion.Meshes.PSK
{
    public class CMeshSection
    {
        public int MaterialIndex;
        public string? MaterialName;
        public ResolvedObject? Material; // UMaterialInterface
        public int FirstIndex;
        public int NumFaces;

        public bool IsValid => MaterialIndex > -1;

        public CMeshSection(FStaticMeshSection section)
        {
            MaterialIndex = -1;
            FirstIndex = section.FirstIndex;
            NumFaces = section.NumTriangles;
        }

        public CMeshSection(FSkelMeshSection section)
        {
            MaterialIndex = -1;
            FirstIndex = section.BaseIndex;
            NumFaces = section.NumTriangles;
        }

        public CMeshSection(int index, FStaticMeshSection section, string? materialName, ResolvedObject? material) : this(section)
        {
            MaterialIndex = index;
            MaterialName = materialName;
            Material = material;
        }

        public CMeshSection(int index, FSkelMeshSection section, string? materialName, ResolvedObject? material) : this(section)
        {
            MaterialIndex = index;
            MaterialName = materialName;
            Material = material;
        }
    }
}
