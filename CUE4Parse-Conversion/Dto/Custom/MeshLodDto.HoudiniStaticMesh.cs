using CommunityToolkit.HighPerformance;
using CUE4Parse.UE4.Assets.Exports.Houdini;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;

namespace CUE4Parse_Conversion.Dto;

public partial class MeshLodDto<TVertex>
{
    internal static MeshLodDto<MeshVertex> FromHoudiniStaticMesh(StaticMeshDto owner, UHoudiniStaticMesh mesh)
    {
        if (mesh.TriangleIndices.Length == 0)
            throw new InvalidOperationException("Houdini static mesh has no triangles");

        var numTriangles = mesh.TriangleIndices.Length;
        var numVertices = numTriangles * 3;

        var vertices = new MeshVertex[numVertices];
        var indices = new uint[numVertices];

        var vertexColors = mesh.bHasColors ? mesh.VertexInstanceColors[..numVertices] : null;

        var uvs = mesh.VertexInstanceUVs.AsSpan().Cast<FVector2D, FMeshUVFloat>();
        var extraUvs = new FMeshUVFloat[Math.Max(0, (int) mesh.NumUVLayers - 1)][];
        for (var i = 0; i < extraUvs.Length; i++)
        {
            extraUvs[i] = uvs.Slice((i + 1) * numVertices, numVertices).ToArray();
        }

        var positionIndices = mesh.TriangleIndices.AsSpan().Cast<FIntVector, int>();
        for (var triangleIndex = 0; triangleIndex < numTriangles; triangleIndex++)
        {
            var firstVertex = triangleIndex * 3;
            var triangle = positionIndices.Slice(firstVertex, 3);
            var faceNormal = FVector.ZeroVector;
            if (!mesh.bHasNormals)
            {
                var p0 = mesh.VertexPositions[triangle[0]];
                faceNormal = FVector.CrossProduct(mesh.VertexPositions[triangle[2]] - p0, mesh.VertexPositions[triangle[1]] - p0).GetSafeNormal();
                faceNormal = faceNormal.IsZero() ? -FVector.UpVector : faceNormal;
            }

            for (var corner = 0; corner < 3; corner++)
            {
                var i = firstVertex + corner;
                var normal = mesh.bHasNormals ? mesh.VertexInstanceNormals[i] : faceNormal;
                FVector tangent;
                var tangentSign = 1f;
                if (mesh.bHasTangents)
                {
                    tangent = mesh.VertexInstanceUTangents[i];
                    tangentSign = FVector.DotProduct(FVector.CrossProduct(normal, tangent), mesh.VertexInstanceVTangents[i]) < 0f ? -1f : 1f;
                }
                else
                {
                    var referenceAxis = MathF.Abs(normal.Z) < 0.999f ? FVector.UpVector : FVector.RightVector;
                    tangent = FVector.CrossProduct(referenceAxis, normal).GetSafeNormal();
                }

                var uv = mesh.NumUVLayers > 0 ? uvs[i] : FMeshUVFloat.ZeroVector;
                vertices[i] = new MeshVertex(mesh.VertexPositions[triangle[corner]], new FVector4(normal, tangentSign), new FVector4(tangent, tangentSign), uv);
                indices[i] = (uint) i;
            }
        }

        MeshSectionDto[] sections;
        if (!mesh.bHasPerFaceMaterials || owner.Materials.Length <= 1)
        {
            sections = [new MeshSectionDto(0, 0, numTriangles, true)];
        }
        else
        {
            var materialGroups = Enumerable.Range(0, numTriangles).GroupBy(i => Math.Clamp(mesh.MaterialIDsPerTriangle[i], 0, owner.Materials.Length - 1)).OrderBy(x => x.Key).ToArray();
            sections = new MeshSectionDto[materialGroups.Length];
            var offset = 0;
            for (var i = 0; i < sections.Length; i++)
            {
                var group = materialGroups[i];
                sections[i] = new MeshSectionDto(group.Key, offset, group.Count(), true);
                foreach (var triangleIndex in group)
                {
                    var firstVertex = (uint) (triangleIndex * 3);
                    indices[offset++] = firstVertex;
                    indices[offset++] = firstVertex + 1;
                    indices[offset++] = firstVertex + 2;
                }
            }
        }

        return new MeshLodDto<MeshVertex>(owner, 0, indices, vertices, sections, extraUvs, vertexColors, 1.0f);
    }
}
