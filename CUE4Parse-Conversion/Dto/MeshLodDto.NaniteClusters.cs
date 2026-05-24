using System.Threading;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;

namespace CUE4Parse_Conversion.Dto;

public partial class MeshLodDto<TVertex>
{
    internal static MeshLodDto<MeshVertex> FromNaniteClusters(StaticMeshDto owner, FCluster[] clusters, int sectionCount, int numTexCoords, int numVertices)
    {
        var sections = new MeshSectionDto[sectionCount];
        for (var i = 0; i < sectionCount; i++)
        {
            sections[i] = new MeshSectionDto(i, 0, 0, true);
        }

        // pass 1: count the number of faces for each section
        Parallel.ForEach(clusters, cluster =>
        {
            if (!cluster.ShouldUseMaterialTable())
            {
                Interlocked.Add(ref sections[cluster.Material0Index].NumFaces, (int) cluster.Material0Length);
                Interlocked.Add(ref sections[cluster.Material1Index].NumFaces, (int) cluster.Material1Length);
                Interlocked.Add(ref sections[cluster.Material2Index].NumFaces, (int) (cluster.NumTris - (cluster.Material1Length + cluster.Material0Length)));
            }
            else foreach (FMaterialRange range in cluster.MaterialRanges)
            {
                Interlocked.Add(ref sections[range.MaterialIndex].NumFaces, (int) range.TriLength);
            }
        });

        // Create trackers for write positions for each material since we are working in parallel.
        var triBufferWriteOffsets = new int[sections.Length];
        for (int i = 1; i < sections.Length; i++)
        {
            triBufferWriteOffsets[i] = sections[i - 1].FirstIndex + sections[i - 1].NumFaces;
            sections[i].FirstIndex = 3 * triBufferWriteOffsets[i];
        }

        var indices = new uint[sections[^1].FirstIndex + sections[^1].NumFaces];
        var extraUvs = new FMeshUVFloat[numTexCoords][];
        var vertices = new MeshVertex[numVertices];
        var vertexColors = new FColor[vertices.Length];

        for (var i = 0; i < extraUvs.Length; i++)
        {
            extraUvs[i] = new FMeshUVFloat[vertices.Length];
        }

        // pass 2: assign vertices and indices
        var vertOffsetTracker = 0u;
        Parallel.ForEach(clusters, cluster =>
        {
            var globalVertOffset = Interlocked.Add(ref vertOffsetTracker, cluster.NumVerts) - cluster.NumVerts;

            for (var i = 0u; i < cluster.Vertices.Length; i++)
            {
                var vertOffset = i + globalVertOffset;

                if (cluster.Vertices[i] is { Attributes: { } attributes } vert)
                {
                    vertices[vertOffset] = new MeshVertex(vert.Pos, attributes, cluster.bHasTangents);

                    for (var j = 0; j < extraUvs.Length; j++)
                    {
                        extraUvs[j][vertOffset].U = attributes.UVs[j + 1].X;
                        extraUvs[j][vertOffset].V = attributes.UVs[j + 1].Y;
                    }

                    vertexColors[vertOffset] = attributes.Color;
                }
                else
                {
                    vertices[vertOffset] = new MeshVertex();
                }
            }

            if (!cluster.ShouldUseMaterialTable())
            {
                WriteIndices(cluster.Material0Index, 0, cluster.Material0Length);
                WriteIndices(cluster.Material1Index, cluster.Material0Length, cluster.Material1Length);
                WriteIndices(cluster.Material2Index, cluster.Material0Length + cluster.Material1Length, cluster.NumTris - (cluster.Material0Length + cluster.Material1Length));
            }
            else foreach (FMaterialRange range in cluster.MaterialRanges)
            {
                WriteIndices(range.MaterialIndex, range.TriStart, range.TriLength);
            }

            void WriteIndices(uint matIndex, uint triStart, uint triLength)
            {
                if (triLength <= 0) return;

                var triBufferWriteOffset = Interlocked.Add(ref triBufferWriteOffsets[matIndex], (int) triLength) - triLength;
                for (long localTriIndex = 0; localTriIndex < triLength; localTriIndex++)
                {
                    indices[triBufferWriteOffset++] = cluster.TriIndices[triStart + localTriIndex].X + globalVertOffset;
                    indices[triBufferWriteOffset++] = cluster.TriIndices[triStart + localTriIndex].Y + globalVertOffset;
                    indices[triBufferWriteOffset++] = cluster.TriIndices[triStart + localTriIndex].Z + globalVertOffset;
                }
            }
        });

        return new MeshLodDto<MeshVertex>(owner, indices, vertices, sections, extraUvs, vertexColors, 1.0f);
    }
}
