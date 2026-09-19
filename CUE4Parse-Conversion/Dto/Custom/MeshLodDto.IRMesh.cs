using System.Runtime.InteropServices;
using CUE4Parse.GameTypes.Nascar.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;

namespace CUE4Parse_Conversion.Dto;

public partial class MeshLodDto<TVertex>
{
    internal static MeshLodDto<MeshVertex> FromIRMesh(StaticMeshDto owner, UIRMesh mesh)
    {
        var vertexCount = mesh.MeshBuffers.Sum(x => x.VertexCount);
        var indexCount = mesh.MeshBuffers.Sum(x => x.IndexCount);
        var uvCount = Math.Min(int.MaxValue, Math.Max(0, mesh.MeshBuffers.Max(x => x.UVChannelCount) - 1));
        var vertices = new MeshVertex[vertexCount];
        var indices = new uint[indexCount];
        var extraUVs = new FMeshUVFloat[uvCount][];
        for (var channel = 0; channel < uvCount; channel++)
        {
            extraUVs[channel] = new FMeshUVFloat[vertexCount];
        }

        var vertexOffsets = new int[mesh.MeshBuffers.Length];
        var indexOffsets = new int[mesh.MeshBuffers.Length];
        var vertexOffset = 0;
        var indexOffset = 0;
        for (var bufferIndex = 0; bufferIndex < mesh.MeshBuffers.Length; bufferIndex++)
        {
            var buffer = mesh.MeshBuffers[bufferIndex];
            vertexOffsets[bufferIndex] = vertexOffset;
            indexOffsets[bufferIndex] = indexOffset;

            var tangents = MemoryMarshal.Cast<byte, uint>(buffer.TangentData);
            var uvs = MemoryMarshal.Cast<byte, FMeshUVFloat>(buffer.UVData);
            for (var vertex = 0; vertex < buffer.VertexCount; vertex++)
            {
                var position = MemoryMarshal.Read<FVector>(buffer.PositionData.AsSpan(vertex * buffer.PositionStride));
                var normal = (FVector4) new FPackedNormal(tangents[vertex * 2]);
                var tangent = (FVector4) new FPackedNormal(tangents[vertex * 2 + 1]);
                var uvOffset = vertex * buffer.UVChannelCount;
                var uv = buffer.UVChannelCount > 0 ? uvs[uvOffset] : FMeshUVFloat.ZeroVector;

                vertices[vertexOffset + vertex] = new MeshVertex(position, normal, tangent, uv);
                for (var channel = 0; channel < Math.Min(uvCount, buffer.UVChannelCount - 1); channel++)
                {
                    extraUVs[channel][vertexOffset + vertex] = uvs[uvOffset + channel + 1];
                }
            }
            switch (buffer.IndexStride)
            {
                case 2:
                {
                    var sourceIndices = MemoryMarshal.Cast<byte, ushort>(buffer.IndexData);
                    for (var index = 0; index < sourceIndices.Length; index++)
                    {
                        indices[indexOffset + index] = sourceIndices[index];
                    }
                    break;
                }
                case 4:
                    MemoryMarshal.Cast<byte, uint>(buffer.IndexData).CopyTo(indices.AsSpan(indexOffset));
                    break;
                default:
                    throw new InvalidDataException($"Unsupported IRMesh index stride {buffer.IndexStride}");
            }

            vertexOffset += buffer.VertexCount;
            indexOffset += buffer.IndexCount;
        }

        var sections = new MeshSectionDto[mesh.Sections.Length];
        for (var sectionIndex = 0; sectionIndex < mesh.Sections.Length; sectionIndex++)
        {
            var section = mesh.Sections[sectionIndex];
            var baseVertex = vertexOffsets[section.BufferIndex] + section.MinVertexIndex;
            var firstIndex = indexOffsets[section.BufferIndex] + section.FirstIndex;
            foreach (ref var index in indices.AsSpan(firstIndex, section.NumTriangles * 3))
            {
                index += (uint) baseVertex;
            }

            sections[sectionIndex] = new MeshSectionDto(section.MaterialIndex, firstIndex, section.NumTriangles, true);
        }

        return new MeshLodDto<MeshVertex>(owner, 0, indices, vertices, sections, extraUVs, vertexColors: (MeshVertexColorDto[]?) null);
    }
}
