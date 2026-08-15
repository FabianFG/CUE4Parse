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
    public readonly bool IsNanite;

    internal string? _suffix;

    private MeshLodDto(MeshDto<TVertex> owner, uint sourceLodIndex, uint[] indices, TVertex[] vertices, MeshSectionDto[] sections, FMeshUVFloat[][] extraUvs, MeshVertexColorDto[]? vertexColors = null, float screenSize = 0.0f, bool isTwoSided = false, bool isNanite = false)
    {
        if (owner.Materials.Length > 0)
        {
            for (var i = 0; i < sections.Length; i++)
            {
                // unfortunately we can't trust these indices
                var materialIndex = Math.Clamp(sections[i].MaterialIndex, 0, owner.Materials.Length - 1);
                sections[i] = new MeshSectionDto(materialIndex, sections[i]);
            }
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
        IsNanite = isNanite;
    }

    private MeshLodDto(MeshDto<TVertex> owner, uint sourceLodIndex, uint[] indices, TVertex[] vertices, MeshSectionDto[] sections, FMeshUVFloat[][] extraUv, FColor[]? vertexColors = null, float screenSize = 0.0f, bool isTwoSided = false, bool isNanite = false)
        : this(owner, sourceLodIndex, indices, vertices, sections, extraUv, vertexColors != null ? [new MeshVertexColorDto("COL0", vertexColors)] : null, screenSize, isTwoSided, isNanite)
    {

    }

    public FBox CalculateLodBounds()
    {
        var min = new FVector(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new FVector(float.MinValue, float.MinValue, float.MinValue);
        foreach (var vert in Vertices)
        {
            var v = vert.Position;
            if (v[0] < min[0]) min[0] = v[0];
            if (v[0] > max[0]) max[0] = v[0];
            if (v[1] < min[1]) min[1] = v[1];
            if (v[1] > max[1]) max[1] = v[1];
            if (v[2] < min[2]) min[2] = v[2];
            if (v[2] > max[2]) max[2] = v[2];
        }

        return new FBox((min + max) / 2.0f, (max - min) / 2.0f);
    }
}
