using CUE4Parse.UE4.Objects.Chaos.GeometryCollection;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Dto;

public partial class MeshLodDto<TVertex>
{
    internal static MeshLodDto<MeshVertex> FromRenderData(StaticMeshDto owner, uint sourceLodIndex, FGeometryCollectionMeshResources resources, FGeometryCollectionMeshDescription description, FGeometryCollection? collection)
    {
        ArgumentNullException.ThrowIfNull(resources.IndexBuffer.Buffer, "LOD has no index buffer");

        var extraUvs = new FMeshUVFloat[Math.Max(0, resources.StaticMeshVertexBuffer.NumTexCoords - 1)][];
        var vertices = new MeshVertex[resources.PositionVertexBuffer.Verts.Length];

        for (var i = 0; i < extraUvs.Length; i++)
        {
            extraUvs[i] = new FMeshUVFloat[vertices.Length];
        }

        FColor[]? vertexColors = null;
        if (resources.ColorVertexBuffer is { Data.Length: > 0 })
        {
            vertexColors = new FColor[vertices.Length]; // we don't need colors that don't belong to any vertex
            Array.Copy(resources.ColorVertexBuffer.Data, vertexColors, vertexColors.Length);
        }
        else if (resources.BoneMapVertexBuffer.BoneMap is { Length: > 0 } && collection?.TryGetAttributeValue<FLinearColor>("BoneColor", "Transform", out var boneColors) == true)
        {
            vertexColors = new FColor[vertices.Length];
            for (var i = 0; i < vertexColors.Length; i++)
            {
                var boneIndex = resources.BoneMapVertexBuffer.BoneMap[i];
                vertexColors[i] = boneColors[boneIndex].ToFColor(true);
            }
        }

        for (var i = 0; i < vertices.Length; i++)
        {
            var pos = resources.PositionVertexBuffer.Verts[i];
            // var boneIndex = resources.BoneMapVertexBuffer.BoneMap[i];
            var uv = resources.StaticMeshVertexBuffer.UV[i];
            vertices[i] = new MeshVertex(pos, uv.Normal[2], uv.Normal[0], uv.UV[0]/*, boneIndex*/);

            for (var j = 0; j < extraUvs.Length; j++)
            {
                extraUvs[j][i].U = uv.UV[j + 1].U;
                extraUvs[j][i].V = uv.UV[j + 1].V;
            }
        }

        var sections = new MeshSectionDto[description.Sections.Length];
        for (var i = 0; i < sections.Length; i++)
        {
            sections[i] = new MeshSectionDto(description.Sections[i]);
        }

        return new MeshLodDto<MeshVertex>(owner, sourceLodIndex, resources.IndexBuffer.Buffer, vertices, sections, extraUvs, vertexColors);
    }

    internal static MeshLodDto<MeshVertex> FromArrayCollection(StaticMeshDto owner, uint sourceLodIndex, FManagedArrayCollection collection)
    {
        var verticesGroup = new FName("Vertices");
        var vertexAttr = collection.GetAttributeValue<FVector>("Vertex", verticesGroup);
        var normalAttr = collection.GetAttributeValue<FVector>("Normal", verticesGroup);
        var tangentUAttr = collection.GetAttributeValue<FVector>("TangentU", verticesGroup);
        var tangentVAttr = collection.GetAttributeValue<FVector>("TangentV", verticesGroup);
        var uvLayer0Att = collection.GetAttributeValue<FVector2D>("UVLayer0", verticesGroup);
        var boneMapAttr = collection.GetAttributeValue<int>("BoneMap", verticesGroup);

        var vertices = new MeshVertex[vertexAttr.Length];
        for (var i = 0; i < vertices.Length; i++)
        {
            var normal = normalAttr[i];
            var tangent = tangentUAttr[i];
            var sign = FVector.DotProduct(FVector.CrossProduct(normal, tangent), tangentVAttr[i]) < 0f ? -1f : 1f;

            var uv = new FMeshUVFloat(uvLayer0Att[i].X, uvLayer0Att[i].Y);
            vertices[i] = new MeshVertex(vertexAttr[i], new FVector4(normal, sign), new FVector4(tangent, sign), uv/*, (ushort) boneMapAttr[i]*/);
        }

        var extraUvs = new FMeshUVFloat[2][];
        var uvLayer1Attr = collection.GetAttributeValue<FVector2D>("UVLayer1", verticesGroup);
        var uvLayer2Attr = collection.GetAttributeValue<FVector2D>("UVLayer2", verticesGroup);
        for (var i = 0; i < extraUvs.Length; i++)
        {
            var attr = i == 0 ? uvLayer1Attr : uvLayer2Attr;
            extraUvs[i] = new FMeshUVFloat[vertices.Length];
            for (var j = 0; j < extraUvs[i].Length; j++)
            {
                extraUvs[i][j] = new FMeshUVFloat(attr[j].X, attr[j].Y);
            }
        }

        FColor[]? vertexColors = null;
        if (collection.TryGetAttributeValue<FLinearColor>("Color", verticesGroup, out var colors) && colors.Length > 0)
        {
            vertexColors = new FColor[vertices.Length]; // we don't need colors that don't belong to any vertex
            for (var i = 0; i < vertexColors.Length; i++)
            {
                vertexColors[i] = colors[i].ToFColor(true);
            }
        }
        else if (collection.TryGetAttributeValue<FLinearColor>("BoneColor", "Transform", out var boneColors))
        {
            vertexColors = new FColor[vertices.Length];
            for (var i = 0; i < vertexColors.Length; i++)
            {
                var boneIndex = boneMapAttr[i];
                vertexColors[i] = boneColors[boneIndex].ToFColor(true);
            }
        }

        var facesGroup = new FName("Faces");
        var indicesAttr = collection.GetAttributeValue<FIntVector>("Indices", facesGroup);
        // var visibleAttr = collection.GetAttributeValue<bool>("Visible", facesGroup);
        var indices = new uint[indicesAttr.Length * 3];
        for (var i = 0; i < indicesAttr.Length; i++)
        {
            // if (!visibleAttr[i]) continue;
            indices[i * 3] = (uint) indicesAttr[i].X;
            indices[i * 3 + 1] = (uint) indicesAttr[i].Y;
            indices[i * 3 + 2] = (uint) indicesAttr[i].Z;
        }

        var sectionsAttr = collection.GetAttributeValue<FGeometryCollectionSection>("Sections", "Material");
        var sections = new MeshSectionDto[sectionsAttr.Length];
        for (var i = 0; i < sections.Length; i++)
        {
            sections[i] = new MeshSectionDto(sectionsAttr[i]);
        }

        return new MeshLodDto<MeshVertex>(owner, sourceLodIndex, indices, vertices, sections, extraUvs, vertexColors);
    }
}
