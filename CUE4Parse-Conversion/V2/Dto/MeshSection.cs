using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;

namespace CUE4Parse_Conversion.V2.Dto;

public struct MeshSection(int index, int firstIndex, int numFaces, bool castShadow)
{
    public readonly int MaterialIndex = index;
    public readonly bool CastShadow = castShadow;
    public int FirstIndex = firstIndex;
    public int NumFaces = numFaces;

    public bool IsValid => MaterialIndex > -1;

    public MeshSection(FStaticMeshSection section) : this(section.MaterialIndex, section.FirstIndex, section.NumTriangles, section.bCastShadow)
    {

    }

    public MeshSection(FSkelMeshSection section) : this(section.MaterialIndex, section.BaseIndex, section.NumTriangles, section.bCastShadow)
    {

    }
}
