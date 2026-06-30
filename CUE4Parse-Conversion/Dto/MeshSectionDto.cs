using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;

namespace CUE4Parse_Conversion.Dto;

public struct MeshSectionDto(int index, int firstIndex, int numFaces, bool castShadow)
{
    public readonly int MaterialIndex = index;
    public readonly bool CastShadow = castShadow;
    public int FirstIndex = firstIndex;
    public int NumFaces = numFaces;

    public bool IsValid => MaterialIndex > -1;

    public MeshSectionDto(int index, MeshSectionDto section) : this(index, section.FirstIndex, section.NumFaces, section.CastShadow)
    {

    }

    public MeshSectionDto(FStaticMeshSection section) : this(section.MaterialIndex, section.FirstIndex, section.NumTriangles, section.bCastShadow)
    {

    }

    public MeshSectionDto(FSkelMeshSection section) : this(section.MaterialIndex, section.BaseIndex, section.NumTriangles, section.bCastShadow)
    {

    }
}
