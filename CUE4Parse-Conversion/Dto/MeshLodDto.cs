using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;

namespace CUE4Parse_Conversion.Dto;

public partial class MeshLodDto<TVertex>(MeshDto<TVertex> owner, uint sourceLodIndex, uint[] indices, TVertex[] vertices, MeshSectionDto[] sections, FMeshUVFloat[][] extraUvs, MeshVertexColorDto[]? vertexColors = null, float screenSize = 0.0f, bool isTwoSided = false) where TVertex : struct, IMeshVertex
{
    public readonly MeshDto<TVertex> Owner = owner;
    public readonly uint SourceLodIndex = sourceLodIndex;
    public readonly uint[] Indices = indices;
    public readonly TVertex[] Vertices = vertices;
    public readonly MeshSectionDto[] Sections = sections;
    public readonly FMeshUVFloat[][] ExtraUvs = extraUvs;
    public readonly MeshVertexColorDto[]? VertexColors = vertexColors;
    public readonly float ScreenSize = screenSize;
    public readonly bool IsTwoSided = isTwoSided;

    private MeshLodDto(MeshDto<TVertex> owner, uint sourceLodIndex, uint[] indices, TVertex[] vertices, MeshSectionDto[] sections, FMeshUVFloat[][] extraUv, FColor[]? vertexColors = null, float screenSize = 0.0f, bool isTwoSided = false)
        : this(owner, sourceLodIndex, indices, vertices, sections, extraUv, vertexColors != null ? [new MeshVertexColorDto("COL0", vertexColors)] : null, screenSize, isTwoSided)
    {

    }
}
