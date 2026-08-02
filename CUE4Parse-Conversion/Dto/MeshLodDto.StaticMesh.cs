using System;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;

namespace CUE4Parse_Conversion.Dto;

public partial class MeshLodDto<TVertex>
{
    internal static MeshLodDto<MeshVertex> FromStaticMesh(StaticMeshDto owner, uint sourceLodIndex, FStaticMeshLODResources lod, float screenSize, USplineMeshComponent? spline = null)
    {
        ArgumentNullException.ThrowIfNull(lod.IndexBuffer?.Buffer, "LOD has no index buffer");
        ArgumentNullException.ThrowIfNull(lod.VertexBuffer, "LOD has no vertex buffer");
        ArgumentNullException.ThrowIfNull(lod.PositionVertexBuffer, "LOD has no position vertex buffer");

        var extraUvs = new FMeshUVFloat[lod.VertexBuffer.NumTexCoords - 1][];
        var vertices = new MeshVertex[lod.PositionVertexBuffer.Verts.Length];

        for (var i = 0; i < extraUvs.Length; i++)
        {
            extraUvs[i] = new FMeshUVFloat[vertices.Length];
        }

        FColor[]? vertexColors = null;
        if (lod.ColorVertexBuffer is { Data.Length: > 0 })
        {
            vertexColors = new FColor[vertices.Length]; // we don't need colors that don't belong to any vertex
            Array.Copy(lod.ColorVertexBuffer.Data, vertexColors, vertexColors.Length);
        }

        for (var i = 0; i < vertices.Length; i++)
        {
            var pos = lod.PositionVertexBuffer.Verts[i];
            if (spline != null) // TODO normals
            {
                var distanceAlong = USplineMeshComponent.GetAxisValueRef(ref pos, spline.ForwardAxis);
                var sliceTransform = spline.CalcSliceTransform(distanceAlong);
                USplineMeshComponent.SetAxisValueRef(ref pos, spline.ForwardAxis, 0f);
                pos = sliceTransform.TransformPosition(pos);
            }

            var uv = lod.VertexBuffer.UV[i];
            vertices[i] = new MeshVertex(pos, uv.Normal[2], uv.Normal[0], uv.UV[0]);

            for (var j = 0; j < extraUvs.Length; j++)
            {
                extraUvs[j][i].U = uv.UV[j + 1].U;
                extraUvs[j][i].V = uv.UV[j + 1].V;
            }
        }

        var sections = new MeshSectionDto[lod.Sections.Length];
        for (var i = 0; i < sections.Length; i++)
        {
            sections[i] = new MeshSectionDto(lod.Sections[i]);
        }

        return new MeshLodDto<MeshVertex>(owner, sourceLodIndex, lod.IndexBuffer.Buffer, vertices, sections, extraUvs, vertexColors, screenSize, lod.CardRepresentationData?.bMostlyTwoSided ?? false);
    }
}
