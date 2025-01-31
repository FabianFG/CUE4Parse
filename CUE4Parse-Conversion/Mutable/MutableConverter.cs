using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;

namespace CUE4Parse_Conversion.Mutable;

public static class MutableConverter
{
    public static bool TryConvert(this FMesh originalMesh, string materialSlotName, USkeleton skeleton, out CSkeletalMesh convertedMesh, List<FMesh>? additionalLods = null)
    {
        convertedMesh = new CSkeletalMesh();

        // convertedMesh.BoundingSphere = new FSphere(0f, 0f, 0f, originalMesh.ImportedBounds.SphereRadius / 2);
        // convertedMesh.BoundingBox = new FBox(
        // originalMesh.ImportedBounds.Origin - originalMesh.ImportedBounds.BoxExtent,
        // originalMesh.ImportedBounds.Origin + originalMesh.ImportedBounds.BoxExtent);

        convertedMesh.LODs.Add(BuildLodObject(originalMesh, materialSlotName));
        if (additionalLods != null)
        {
            foreach (var lod in additionalLods)
                convertedMesh.LODs.Add(BuildLodObject(lod, materialSlotName));
        }

        if (skeleton.TryConvert(out var bones, out var box))
            convertedMesh.RefSkeleton.AddRange(bones);

        convertedMesh.FinalizeMesh();
        return true;
    }

    private static CSkelMeshLod BuildLodObject(FMesh originalMesh, string materialSlotName)
    {
        GetVertexBuffer(EMeshBufferSemantic.Position, originalMesh, out var positionChannel, out var positionBuffer);
        GetVertexBuffer(EMeshBufferSemantic.Normal, originalMesh, out var normalChannel, out var normalBuffer);
        GetVertexBuffer(EMeshBufferSemantic.Tangent, originalMesh, out var tangentChannel, out var tangentBuffer);
        GetVertexBuffer(EMeshBufferSemantic.TexCoords, originalMesh, out var texCoord0Channel, out var texCoord0Buffer);
        GetVertexBuffer(EMeshBufferSemantic.TexCoords, originalMesh, out var texCoord1Channel, out var texCoord1Buffer, 1);
        GetVertexBuffer(EMeshBufferSemantic.TexCoords, originalMesh, out var texCoord2Channel, out var texCoord2Buffer, 2);
        GetVertexBuffer(EMeshBufferSemantic.Color, originalMesh, out var colorChannel, out var colorBuffer);
        GetVertexBuffer(EMeshBufferSemantic.BoneIndices, originalMesh, out var boneIndexChannel, out var boneIndexBuffer);
        GetVertexBuffer(EMeshBufferSemantic.BoneWeights, originalMesh, out var weightChannel, out var weightBuffer);
        
        var indices = new uint[originalMesh.IndexBuffers.ElementCount];
        for (var i = 0; i < indices.Length; i++)
        {
            indices[i] = BitConverter.ToUInt32(originalMesh.IndexBuffers.Buffers[0].Data,
                i * (int) originalMesh.IndexBuffers.Buffers[0].ElementSize);
        }

        var numTexCoords = texCoord1Buffer == null ? 1 : texCoord2Buffer == null ? 2 : 3;
        
        var skeletalMeshLod = new CSkelMeshLod
        {
            NumTexCoords = numTexCoords,
            HasNormals = true,
            HasTangents = tangentBuffer != null,
            Indices = new Lazy<FRawStaticIndexBuffer>(() => new FRawStaticIndexBuffer
            {
                Indices32 = indices
            }),
            Sections = new Lazy<CMeshSection[]>(() =>
            {
                var sections = new CMeshSection[1];
                sections[0] = new CMeshSection(0, 0, Convert.ToInt32(originalMesh.IndexBuffers.ElementCount / 3), materialSlotName, null);
                return sections;
            })
        };
        
        var vertexCount = originalMesh.VertexBuffers.ElementCount;
        skeletalMeshLod.AllocateVerts(Convert.ToInt32(originalMesh.VertexBuffers.ElementCount));
        
        ushort[]? boneMap = null;

        if (colorBuffer?.ElementSize == vertexCount)
            skeletalMeshLod.AllocateVertexColorBuffer();

        for (var vert = 0; vert < vertexCount; vert++)
        {
            skeletalMeshLod.Verts[vert].UV = GetVertexCoordinates(texCoord0Buffer, texCoord0Channel, vert);

            if (numTexCoords > 1)
            {
                skeletalMeshLod.ExtraUV.Value[0][vert] = GetVertexCoordinates(texCoord1Buffer, texCoord1Channel, vert);
                if (numTexCoords > 2)
                    skeletalMeshLod.ExtraUV.Value[1][vert] = GetVertexCoordinates(texCoord2Buffer, texCoord2Channel, vert);
            }

            skeletalMeshLod.Verts[vert].Position = GetVertexPosition(positionBuffer, positionChannel, vert);
            skeletalMeshLod.Verts[vert].Normal = GetVertexNormal(normalBuffer, normalChannel, vert);
            
            if (skeletalMeshLod.HasTangents)
                skeletalMeshLod.Verts[vert].Tangent = GetVertexTangent(tangentBuffer, tangentChannel, vert);
            
            if (skeletalMeshLod.VertexColors != null)
                skeletalMeshLod.VertexColors[vert] = GetVertexColor(colorBuffer, colorChannel, vert);

            if (boneIndexBuffer == null) continue;
            foreach (var (boneIndex, weight) in GetVertexWeights(weightBuffer, boneIndexChannel, weightChannel, vert))
            {
                if (weight != 0)
                {
                    skeletalMeshLod.Verts[vert].AddInfluence(boneIndex, weight);
                }
            }
        }

        return skeletalMeshLod;
    }
    
    private static FVector GetVertexPosition(FMeshBuffer positionBuffer, FMeshBufferChannel positionChannel, int vertIndex)
    {
        var vertPosX = BitConverter.ToSingle(positionBuffer.Data, vertIndex * ((int) positionBuffer.ElementSize) + positionChannel.Offset);
        var vertPosY = BitConverter.ToSingle(positionBuffer.Data, vertIndex * ((int) positionBuffer.ElementSize) + positionChannel.Offset + 4);
        var vertPosZ = BitConverter.ToSingle(positionBuffer.Data, vertIndex * ((int) positionBuffer.ElementSize) + positionChannel.Offset + 8);

        return new FVector(vertPosX, vertPosY, vertPosZ);
    }

    private static FPackedNormal GetVertexNormal(FMeshBuffer normalBuffer, FMeshBufferChannel normalChannel, int vertIndex)
    {
        var vertNormX = ((normalBuffer.Data[vertIndex * ((int) normalBuffer.ElementSize) + normalChannel.Offset] * 2) / 256) -1;
        var vertNormY = ((normalBuffer.Data[vertIndex * ((int) normalBuffer.ElementSize) + normalChannel.Offset + 1] * 2) / 256) -1;
        var vertNormZ = ((normalBuffer.Data[vertIndex * ((int) normalBuffer.ElementSize) + normalChannel.Offset + 2] * 2) / 256) -1;
        var vertNormSign = ((normalBuffer.Data[vertIndex * ((int) normalBuffer.ElementSize) + normalChannel.Offset + 3] * 2) / 256) -1; // Not sure what to do with this yet...
        
        return new FPackedNormal(new FVector4(vertNormX, vertNormY, vertNormZ, vertNormSign));
    }

    private static FPackedNormal GetVertexTangent(FMeshBuffer tangentBuffer, FMeshBufferChannel tangentChannel, int vertIndex)
    {
        var vertTangentX = ((tangentBuffer.Data[vertIndex * ((int) tangentBuffer.ElementSize) + tangentChannel.Offset] * 2) / 256) -1;
        var vertTangentY = ((tangentBuffer.Data[vertIndex * ((int) tangentBuffer.ElementSize) + tangentChannel.Offset + 1] * 2) / 256) -1;
        var vertTangentZ = ((tangentBuffer.Data[vertIndex * ((int) tangentBuffer.ElementSize) + tangentChannel.Offset + 2] * 2) / 256) -1;
        var vertTangentW = ((tangentBuffer.Data[vertIndex * ((int) tangentBuffer.ElementSize) + tangentChannel.Offset + 3] * 2) / 256) -1;
        
        return new FPackedNormal(new FVector4(vertTangentX, vertTangentY, vertTangentZ, vertTangentW));
    }

    private static FMeshUVFloat GetVertexCoordinates(FMeshBuffer coordinateBuffer, FMeshBufferChannel coordinateChannel, int vertIndex)
    {
        var vertTexCoordX = BitConverter.ToSingle(coordinateBuffer.Data, vertIndex * ((int) coordinateBuffer.ElementSize) + coordinateChannel.Offset);
        var vertTexCoordY = BitConverter.ToSingle(coordinateBuffer.Data, vertIndex * ((int) coordinateBuffer.ElementSize) + coordinateChannel.Offset + 4);

        return new FMeshUVFloat(vertTexCoordX, vertTexCoordY);
    }

    private static FColor GetVertexColor(FMeshBuffer colorBuffer, FMeshBufferChannel colorChannel, int vertIndex)
    {
        var colorR = colorBuffer.Data[vertIndex * ((int) colorBuffer.ElementSize) + colorChannel.Offset];
        var colorG = colorBuffer.Data[vertIndex * ((int) colorBuffer.ElementSize) + colorChannel.Offset + 1];
        var colorB = colorBuffer.Data[vertIndex * ((int) colorBuffer.ElementSize) + colorChannel.Offset + 2];
        var colorA = colorBuffer.Data[vertIndex * ((int) colorBuffer.ElementSize) + colorChannel.Offset + 3];

        return new FColor(colorR, colorG, colorB, colorA);
    }
    
    
    private static List<Tuple<short, byte>> GetVertexWeights(FMeshBuffer weightBuffer, FMeshBufferChannel boneIndexChannel, FMeshBufferChannel weightChannel, int vertIndex)
    {
        List<Tuple<short, byte>> weightList = [];

        for (int i = 0; i < boneIndexChannel.ComponentCount; i++)
        {
            var boneIndex = weightBuffer.Data[vertIndex * ((int)weightBuffer.ElementSize) + boneIndexChannel.Offset + i];
            var weight = weightBuffer.Data[vertIndex * ((int)weightBuffer.ElementSize) + weightChannel.Offset + i];
            weightList.Add(new Tuple<short, byte>(boneIndex, weight));
        }

        return weightList;
    }
    
    private static void GetVertexBuffer(EMeshBufferSemantic bufferType, FMesh mesh, out FMeshBufferChannel bufferChannel,
        out FMeshBuffer? vertexBuffer, int semanticIndex = 0)
    {
        bufferChannel = null;
        vertexBuffer = null;
        foreach (var buffer in mesh.VertexBuffers.Buffers)
        {
            bufferChannel = buffer.Channels.FirstOrDefault(channel => channel.Semantic == bufferType && channel.SemanticIndex == semanticIndex, null);
            if (bufferChannel == null) continue;
            vertexBuffer = buffer;
            return;
        }
    }

    private static void UnpackNormals(FPackedNormal[] normal, CMeshVertex v)
    {
        // tangents: convert to FVector (unpack) then cast to CVec3
        v.Tangent = normal[0];
        v.Normal = normal[2];

        // new UE3 version - binormal is not serialized and restored in vertex shader
        if (normal[1] is not null && normal[1].Data != 0)
        {
            throw new NotImplementedException();
        }
    }
}