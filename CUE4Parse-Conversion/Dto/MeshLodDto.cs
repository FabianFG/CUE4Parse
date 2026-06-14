using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;

namespace CUE4Parse_Conversion.Dto;

public partial class MeshLodDto<TVertex> where TVertex : struct, IMeshVertex
{
    public readonly MeshDto<TVertex> Owner;
    public readonly uint SourceLodIndex;
    public readonly uint[] Indices;
    public readonly TVertex[] Vertices;
    public readonly MeshSectionDto[] Sections;
    public readonly FMeshUVFloat[][] ExtraUvs;
    public readonly MeshVertexColorDto[]? VertexColors;
    public readonly float ScreenSize;
    public readonly bool IsTwoSided;

    private MeshLodDto(MeshDto<TVertex> owner, uint sourceLodIndex, uint[] indices, TVertex[] vertices, MeshSectionDto[] sections, FMeshUVFloat[][] extraUvs, MeshVertexColorDto[]? vertexColors = null, float screenSize = 0.0f, bool isTwoSided = false)
    {
        for (var i = 0; i < sections.Length; i++)
        {
            // unfortunately we can't trust these indices
            var materialIndex = Math.Clamp(sections[i].MaterialIndex, 0, owner.Materials.Length - 1);
            sections[i] = new MeshSectionDto(materialIndex, sections[i]);
        }

        Owner = owner;
        SourceLodIndex = sourceLodIndex;
        Indices = indices;
        Vertices = vertices;
        Sections = sections;
        ExtraUvs = extraUvs;
        VertexColors = vertexColors;
        ScreenSize = screenSize;
        IsTwoSided = isTwoSided;
    }

    private MeshLodDto(MeshDto<TVertex> owner, uint sourceLodIndex, uint[] indices, TVertex[] vertices, MeshSectionDto[] sections, FMeshUVFloat[][] extraUv, FColor[]? vertexColors = null, float screenSize = 0.0f, bool isTwoSided = false)
        : this(owner, sourceLodIndex, indices, vertices, sections, extraUv, vertexColors != null ? [new MeshVertexColorDto("COL0", vertexColors)] : null, screenSize, isTwoSided)
    {

    }
}
