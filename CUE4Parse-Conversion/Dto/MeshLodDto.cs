using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;

namespace CUE4Parse_Conversion.Dto;

public partial class MeshLodDto<TVertex> where TVertex : struct, IMeshVertex
{
    public readonly MeshDto<TVertex> Owner;
    /// <summary>
    /// Index into the original mesh LOD array, or uint.MaxValue for the Nanite LOD.
    /// </summary>
    public readonly uint SourceLodIndex;
    public readonly uint[] Indices;
    public readonly TVertex[] Vertices;
    public readonly MeshSectionDto[] Sections;
    public readonly FMeshUVFloat[][] ExtraUvs;
    public readonly MeshVertexColorDto[]? VertexColors;
    public readonly float ScreenSize;
    public readonly bool IsTwoSided;
    public bool IsNanite => SourceLodIndex == uint.MaxValue;

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
    }

    private MeshLodDto(MeshDto<TVertex> owner, uint sourceLodIndex, uint[] indices, TVertex[] vertices, MeshSectionDto[] sections, FMeshUVFloat[][] extraUv, FColor[]? vertexColors = null, float screenSize = 0.0f, bool isTwoSided = false)
        : this(owner, sourceLodIndex, indices, vertices, sections, extraUv, vertexColors != null ? [new MeshVertexColorDto("COL0", vertexColors)] : null, screenSize, isTwoSided)
    {

    }

    public FBox CalculateLodBounds()
    {
        if (Vertices.Length == 0) return new FBox(FVector.ZeroVector, FVector.OneVector);

        var min = new FVector(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new FVector(float.MinValue, float.MinValue, float.MinValue);
        foreach (var vert in Vertices)
        {
            var v = vert.Position;
            if (v.X < min.X) min.X = v.X;
            if (v.X > max.X) max.X = v.X;
            if (v.Y < min.Y) min.Y = v.Y;
            if (v.Y > max.Y) max.Y = v.Y;
            if (v.Z < min.Z) min.Z = v.Z;
            if (v.Z > max.Z) max.Z = v.Z;
        }

        return new FBox(min, max);
    }
}
