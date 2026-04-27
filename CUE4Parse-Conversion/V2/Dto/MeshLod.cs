using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse_Conversion.Landscape;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;

namespace CUE4Parse_Conversion.V2.Dto;

public class MeshLod<TVertex>(Mesh<TVertex> owner, uint[] indices, TVertex[] vertices, MeshSection[] sections, FMeshUVFloat[][] extraUvs, MeshVertexColor[]? vertexColors = null, float screenSize = 0.0f, bool isTwoSided = false) where TVertex : struct, IMeshVertex
{
    public readonly Mesh<TVertex> Owner = owner;
    public readonly uint[] Indices = indices;
    public readonly TVertex[] Vertices = vertices;
    public readonly MeshSection[] Sections = sections;
    public readonly FMeshUVFloat[][] ExtraUvs = extraUvs;
    public readonly MeshVertexColor[]? VertexColors = vertexColors;
    public readonly float ScreenSize = screenSize;
    public readonly bool IsTwoSided = isTwoSided;

    private MeshLod(Mesh<TVertex> owner, uint[] indices, TVertex[] vertices, MeshSection[] sections, FMeshUVFloat[][] extraUv, FColor[]? vertexColors = null, float screenSize = 0.0f, bool isTwoSided = false)
        : this(owner, indices, vertices, sections, extraUv, vertexColors != null ? [new MeshVertexColor("COL0", vertexColors)] : null, screenSize, isTwoSided)
    {

    }

    internal static MeshLod<MeshVertex> FromStaticMesh(StaticMesh owner, FStaticMeshLODResources lod, float screenSize, USplineMeshComponent? spline = null)
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

        var sections = new MeshSection[lod.Sections.Length];
        for (var i = 0; i < sections.Length; i++)
        {
            sections[i] = new MeshSection(lod.Sections[i]);
        }

        return new MeshLod<MeshVertex>(owner, lod.IndexBuffer.Buffer, vertices, sections, extraUvs, vertexColors, screenSize, lod.CardRepresentationData?.bMostlyTwoSided ?? false);
    }

    internal static MeshLod<MeshVertex> FromNaniteClusters(StaticMesh owner, FCluster[] clusters, int sectionCount, int numTexCoords, int numVertices)
    {
        var sections = new MeshSection[sectionCount];
        for (var i = 0; i < sectionCount; i++)
        {
            sections[i] = new MeshSection(i, 0, 0, true);
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

        return new MeshLod<MeshVertex>(owner, indices, vertices, sections, extraUvs, vertexColors, 1.0f);
    }

    internal static MeshLod<SkinnedMeshVertex> FromSkeletalMesh(SkeletalMesh owner, FStaticLODModel lod, float screenSize)
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

        var extraUvs = new FMeshUVFloat[lod.NumTexCoords - 1][];
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

        var sections = new MeshSection[lod.Sections.Length];
        for (var i = 0; i < sections.Length; i++)
        {
            sections[i] = new MeshSection(lod.Sections[i]);
        }

        return new MeshLod<SkinnedMeshVertex>(owner, lod.Indices.Buffer, vertices, sections, extraUvs, vertexColors, screenSize);
    }

    internal static MeshLod<MeshVertex> FromLandscapeMesh(StaticMesh owner, ULandscapeComponent[] components, int sizeQuads, SKBitmap? normalTexture = null, Image<L16>? heightmapTexture = null)
    {
        var componentSizeQuads = ((sizeQuads + 1) >> 0 /*Landscape->ExportLOD*/) - 1;
        var scale = (float)componentSizeQuads / sizeQuads;
        var componentVertexCount = (componentSizeQuads + 1) * (componentSizeQuads + 1);

        // https://github.com/EpicGames/UnrealEngine/blob/5de4acb1f05e289620e0a66308ebe959a4d63468/Engine/Source/Editor/UnrealEd/Private/Fbx/FbxMainExport.cpp#L4657
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        var numFaces = components.Length * (componentSizeQuads * componentSizeQuads) * 2;
        var indices = new List<uint>(numFaces * 3);
        for (var i = 0; i < components.Length; i++)
        {
            var baseVertIndex = i * componentVertexCount;
            for (var y = 0; y < componentSizeQuads; y++)
            for (var x = 0; x < componentSizeQuads; x++)
            {
                if (true) // (VisibilityData[BaseVertIndex + Y * (ComponentSizeQuads + 1) + X] < VisThreshold)
                {
                    var w1 = baseVertIndex + (x + 0) + (y + 0) * (componentSizeQuads + 1);
                    var w2 = baseVertIndex + (x + 1) + (y + 1) * (componentSizeQuads + 1);
                    var w3 = baseVertIndex + (x + 1) + (y + 0) * (componentSizeQuads + 1);

                    indices.Add((uint)w1);
                    indices.Add((uint)w2);
                    indices.Add((uint)w3);

                    var w4 = baseVertIndex + (x + 0) + (y + 0) * (componentSizeQuads + 1);
                    var w5 = baseVertIndex + (x + 0) + (y + 1) * (componentSizeQuads + 1);
                    var w6 = baseVertIndex + (x + 1) + (y + 1) * (componentSizeQuads + 1);

                    indices.Add((uint)w4);
                    indices.Add((uint)w5);
                    indices.Add((uint)w6);
                }
            }

            components[i].GetComponentExtent(ref minX, ref minY, ref maxX, ref maxY);
        }

        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        var uvScale = FMeshUVFloat.OneVector / new FMeshUVFloat(width, height);

        var extraUvs = new FMeshUVFloat[1][];
        var vertices = new MeshVertex[components.Length * componentVertexCount];
        MeshVertexColor[]? vertexColors = null;

        for (var i = 0; i < extraUvs.Length; i++)
        {
            extraUvs[i] = new FMeshUVFloat[vertices.Length];
        }

        // if (flags.HasFlag(ELandscapeExportFlags.Weightmap))
        // {
        //     WeightmapTextures = new ConcurrentDictionary<string, SKBitmap>();
        // }

        for (var i = 0; i < components.Length; i++)
        {
            var cdi = new FLandscapeComponentDataInterface(components[i], 0);
            cdi.EnsureWeightmapTextureDataCache();

            var weightMapAllocs = cdi.Component.GetWeightmapLayerAllocations();
            var compTransform = cdi.Component.GetComponentTransform();
            var relLoc = cdi.Component.GetRelativeLocation();

            var baseVertIndex = i * componentVertexCount;

            Parallel.For(0, componentVertexCount, vertIndex =>
            {
                cdi.VertexIndexToXY(vertIndex, out var vertX, out var vertY);

                var textureUv = new FMeshUVFloat(vertX * scale + cdi.Component.SectionBaseX, vertY * scale + cdi.Component.SectionBaseY);
                var textureUv2 = new TIntVector2<int>((int)textureUv.U - minX, (int)textureUv.V - minY);

                heightmapTexture?.ProcessPixelRows(accessor =>
                {
                    var pixelRow = accessor.GetRowSpan(textureUv2.Y);
                    pixelRow[textureUv2.X] = new L16((ushort)(cdi.GetVertex(vertX, vertY) + relLoc).Z);
                });
                // HeightmapTexture[textureUv2.X + textureUv2.Y * width] = new L16((ushort) (cdi.GetVertex(vertX, vertY) + relLoc).Z);

                foreach (var allocationInfo in weightMapAllocs)
                {
                    var weight = cdi.GetLayerWeight(vertX, vertY, allocationInfo);
                    if (weight == 0) continue;

                    var layerName = allocationInfo.GetLayerName();
                    // if (!vertexColors.ContainsKey(layerName))
                    // {
                    //     vertexColors.TryAdd(layerName, new FColor[vertices.Length]);
                    // }
                    //
                    // vertexColors[layerName][baseVertIndex + vertIndex] = new FColor(weight, weight, weight, weight);

                    // if (WeightmapTextures != null)
                    // {
                    //     if (!WeightmapTextures.ContainsKey(layerName))
                    //     {
                    //         WeightmapTextures.TryAdd(layerName, new SKBitmap(width, height, SKColorType.Gray8, SKAlphaType.Unpremul));
                    //     }
                    //
                    //     unsafe
                    //     {
                    //         var pixels = (byte*)WeightmapTextures[layerName].GetPixels();
                    //         pixels[textureUv2.Y * width + textureUv2.X] = weight;
                    //     }
                    // }
                }

                var position = cdi.GetLocalVertex(vertX, vertY) + relLoc;
                cdi.GetLocalTangentVectors(vertIndex, out var tangentX, out _, out var normal);

                normal /= compTransform.Scale3D;
                normal.Normalize();
                FVector4.AsFVector(ref tangentX) /= compTransform.Scale3D;
                FVector4.AsFVector(ref tangentX).Normalize();

                vertices[baseVertIndex + vertIndex] = new MeshVertex(position, normal, tangentX, textureUv);
                extraUvs[0][baseVertIndex + vertIndex] = (textureUv - new FMeshUVFloat(minX, minY)) * uvScale;

                if (normalTexture != null)
                {
                    unsafe
                    {
                        var pixels = (byte*)normalTexture.GetPixels();
                        var pixelX = textureUv2.X;
                        var pixelY = textureUv2.Y;
                        var index = pixelY * width + pixelX;
                        pixels[index * 4 + 2] = (byte)(normal.X * 127 + 128);
                        pixels[index * 4 + 1] = (byte)(normal.Y * 127 + 128);
                        pixels[index * 4 + 0] = (byte)(normal.Z * 127 + 128);
                        pixels[index * 4 + 3] = 255;
                    }
                }
            });
        }

        return new MeshLod<MeshVertex>(owner, indices.ToArray(), vertices, [new MeshSection(0, 0, numFaces, false)], extraUvs, vertexColors, 1.0f);
    }
}
