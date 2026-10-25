using System.Numerics.Tensors;
using CommunityToolkit.HighPerformance;
using CUE4Parse.GameTypes.Nascar.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using GenericReader;

namespace CUE4Parse_Conversion.Dto;

public partial class MeshLodDto<TVertex>
{
    internal static MeshLodDto<MeshVertex> FromIRMesh(StaticMeshDto owner, UIRMesh originalMesh, int partIndex)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(partIndex, originalMesh.Info.PartCount);

        var partSections = originalMesh.Sections.Where(x => x.PartIndex == partIndex).ToArray();
        var minLod = partSections.Min(x => x.LOD);
        partSections = partSections.Where(x => x.LOD == minLod).ToArray();

        var vertCount = partSections.Max(x => x.MaxVertexIndex) + 1;
        var triCount = partSections[^1].FirstIndex + partSections[^1].NumTriangles * 3;

        var part = originalMesh.PartsBuffers[partIndex];
        var numTexCoords = part.Info.NumCoords;
        var numVerts = vertCount;

        var vertices = new MeshVertex[numVerts];
        var extraUvs = new FMeshUVFloat[numTexCoords - 1][];
        for (var i = 0; i < extraUvs.Length; i++)
        {
            extraUvs[i] = new FMeshUVFloat[vertices.Length];
        }

        var indexBuffer = new uint[triCount];
        if (part.IndexStride == 2)
        {
            var span = part.IndexData.AsSpan(0, triCount * 2).Cast<byte, ushort>();
            TensorPrimitives.ConvertChecked(span, indexBuffer);
        }
        else
        {
            var span = part.IndexData.AsSpan(0, triCount * 4).Cast<byte, uint>();
            span.CopyTo(indexBuffer);
        }

        var tangents = part.TangentData.AsSpan().Cast<byte, FIRMeshNormals>();
        var uvs = part.UVData.AsSpan().Cast<byte, FMeshUVFloat>();

        var posStride = part.PositionStride;
        var posAr = new GenericBufferReader(part.PositionData);

        // not sure how to correctly handle case where ScaledUV is true, as in that case first uv for each vertex is
        // FHalfVector4 (which looks like a scale factor), but idk what to do with it, so for now we skip it
        var uvAdditionalOffset = part.Info.ScaledUV ? 1 : 0;
        for (int i = 0, uvOffset = 0, posOffset = 0; i < numVerts; i++, uvOffset += numTexCoords + uvAdditionalOffset, posOffset += posStride)
        {
            posAr.Position = posOffset;
            var pos = posAr.Read<FVector>();
            var uvSpan = uvs.Slice(uvOffset + uvAdditionalOffset, numTexCoords);

            vertices[i] = new MeshVertex(pos, tangents[i].X, tangents[i].Z, uvSpan[0]);

            for (var j = 0; j < extraUvs.Length; j++)
            {
                extraUvs[j][i] = uvSpan[j + 1];
            }
        }

        var sections = new MeshSectionDto[partSections.Length];
        for (int i = 0; i < partSections.Length; i++)
        {
            var sec = partSections[i];
            sections[i] = new MeshSectionDto(sec.MaterialIndex, sec.FirstIndex, sec.NumTriangles, true);
            if (sec.FirstIndex == 0) continue;
            var span = indexBuffer.AsSpan(sec.FirstIndex, sec.NumTriangles * 3);
            TensorPrimitives.Add(span, (uint)sec.MinVertexIndex, span);
        }

        return new MeshLodDto<MeshVertex>(owner, 0, indexBuffer, vertices, sections, extraUvs, null, 1.0f, true);
    }
}
