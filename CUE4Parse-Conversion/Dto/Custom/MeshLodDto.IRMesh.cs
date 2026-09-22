using System.Runtime.InteropServices;
using CUE4Parse.GameTypes.Nascar.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;

namespace CUE4Parse_Conversion.Dto;

public partial class MeshLodDto<TVertex>
{
    internal static MeshLodDto<MeshVertex> FromIRMesh(StaticMeshDto owner, UIRMesh mesh, int bufferIndex = -1)
    {
        var firstBuffer = bufferIndex < 0 ? 0 : bufferIndex;
        var bufferCount = bufferIndex < 0 ? mesh.MeshBuffers.Length : 1;
        var buffers = mesh.MeshBuffers.AsSpan(firstBuffer, bufferCount);
        var vertexCount = 0;
        var indexCount = 0;
        var uvChannelCount = 0;
        foreach (var buffer in buffers)
        {
            vertexCount += buffer.VertexCount;
            indexCount += buffer.IndexCount;
            uvChannelCount = Math.Max(uvChannelCount, buffer.UVChannelCount);
        }

        var uvCount = Math.Max(0, uvChannelCount - 1);
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
        for (var sourceBufferIndex = firstBuffer; sourceBufferIndex < firstBuffer + bufferCount; sourceBufferIndex++)
        {
            var buffer = mesh.MeshBuffers[sourceBufferIndex];
            vertexOffsets[sourceBufferIndex] = vertexOffset;
            indexOffsets[sourceBufferIndex] = indexOffset;

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

        var sourceSections = bufferIndex < 0 ? mesh.Sections : [.. mesh.Sections.Where(section => section.BufferIndex == bufferIndex)];
        var sections = new MeshSectionDto[sourceSections.Length];
        for (var sectionIndex = 0; sectionIndex < sourceSections.Length; sectionIndex++)
        {
            var section = sourceSections[sectionIndex];
            var baseVertex = vertexOffsets[section.BufferIndex] + section.MinVertexIndex;
            var firstIndex = indexOffsets[section.BufferIndex] + section.FirstIndex;
            foreach (ref var index in indices.AsSpan(firstIndex, section.NumTriangles * 3))
            {
                index += (uint) baseVertex;
            }

            sections[sectionIndex] = new MeshSectionDto(section.MaterialIndex, firstIndex, section.NumTriangles, true);
        }

        return new MeshLodDto<MeshVertex>(owner, (uint) Math.Max(0, bufferIndex), indices, vertices, sections, extraUVs, vertexColors: (MeshVertexColorDto[]?) null);
    }
}
