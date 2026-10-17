using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;

namespace CUE4Parse_Conversion.Dto;

public partial class MeshLodDto<TVertex>
{
    internal static MeshLodDto<SkinnedMeshVertex> FromSkeletalMesh(SkeletalMeshDto owner, uint sourceLodIndex, FStaticLODModel lod, float screenSize)
    {
        ArgumentNullException.ThrowIfNull(lod.Indices?.Buffer, "LOD has no index buffer");

        var chunkIndex = 0;
        var chunkVertexIndex = 0;
        var lastChunkVertex = -1L;
        var boneMap = Array.Empty<ushort>();
        var bUseVerticesFromSections = false;

        var vertexCount = lod.VertexBufferGPUSkin.GetVertexCount();
        if (vertexCount == 0 && lod.Sections.Length > 0 && lod.Sections[0].SoftVertices.Length > 0)
        {
            bUseVerticesFromSections = true;
            foreach (var section in lod.Sections)
            {
                vertexCount += section.SoftVertices.Length;
            }
        }

        var extraUvs = new FMeshUVFloat[Math.Max(0, lod.NumTexCoords - 1)][];
        var vertices = new SkinnedMeshVertex[vertexCount];

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
        else if (bUseVerticesFromSections)
        {
            vertexColors = new FColor[vertices.Length]; // vertices from sections have their color baked into the vertex struct
        }

        for (var i = 0; i < vertices.Length; i++)
        {
            while (i >= lastChunkVertex) // this will fix any issues with empty chunks or sections
            {
                if (lod.Chunks.Length > 0) // proceed to next chunk or section
                {
                    // pre-UE4.13 code: chunks
                    var c = lod.Chunks[chunkIndex++];
                    lastChunkVertex = c.BaseVertexIndex + c.NumRigidVertices + c.NumSoftVertices;
                    boneMap = c.BoneMap;
                }
                else
                {
                    // UE4.13+ code: chunk information migrated to sections
                    var s = lod.Sections[chunkIndex++];
                    lastChunkVertex = s.BaseVertexIndex + s.NumVertices;
                    boneMap = s.BoneMap;
                }

                chunkVertexIndex = 0;
            }

            FSkelMeshVertexBase vertex;
            if (bUseVerticesFromSections)
            {
                var v = lod.Sections[chunkIndex].SoftVertices[chunkVertexIndex++];
                vertex = v;
                if (vertexColors != null)
                {
                    vertexColors[i] = v.Color;
                }
            }
            else if (!lod.VertexBufferGPUSkin.bUseFullPrecisionUVs)
            {
                vertex = lod.VertexBufferGPUSkin.VertsHalf[i];
            }
            else
            {
                vertex = lod.VertexBufferGPUSkin.VertsFloat[i];
            }

            vertices[i] = new SkinnedMeshVertex(vertex, boneMap);

            for (var j = 0; j < extraUvs.Length; j++)
            {
                extraUvs[j][i].U = vertex.UVs[j + 1].U;
                extraUvs[j][i].V = vertex.UVs[j + 1].V;
            }
        }

        var sectionCount = 0;
        var indexCount = 0;
        foreach (var section in lod.Sections)
        {
            if (section.bDisabled)
                continue;

            sectionCount++;
            indexCount += section.NumTriangles * 3;
        }

        var indices = lod.Indices.Buffer;
        var hasDisabledSections = sectionCount != lod.Sections.Length;
        if (hasDisabledSections)
            indices = new uint[indexCount];

        var sections = new MeshSectionDto[sectionCount];
        var sectionIndex = 0;
        var firstIndex = 0;
        foreach (var sourceSection in lod.Sections)
        {
            if (sourceSection.bDisabled)
                continue;

            var section = new MeshSectionDto(sourceSection);
            if (hasDisabledSections)
            {
                var count = section.NumFaces * 3;
                Array.Copy(lod.Indices.Buffer, section.FirstIndex, indices, firstIndex, count);
                section.FirstIndex = firstIndex;
                firstIndex += count;
            }

            sections[sectionIndex++] = section;
        }

        return new MeshLodDto<SkinnedMeshVertex>(owner, sourceLodIndex, indices, vertices, sections, extraUvs, vertexColors, screenSize);
    }
}
