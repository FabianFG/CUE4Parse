using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse_Conversion.Landscape;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Assets.Exports.Nanite;
using System.Threading;
using Serilog;

namespace CUE4Parse_Conversion.Meshes;

public static class MeshConverter
{
    public static bool TryConvert(this USkeleton originalSkeleton, out List<CSkelMeshBone> bones, out FBox box)
    {
        bones = new List<CSkelMeshBone>();
        box = new FBox();
        for (var i = 0; i < originalSkeleton.ReferenceSkeleton.FinalRefBoneInfo.Length; i++)
        {
            var skeletalMeshBone = new CSkelMeshBone
            {
                Name = originalSkeleton.ReferenceSkeleton.FinalRefBoneInfo[i].Name,
                ParentIndex = originalSkeleton.ReferenceSkeleton.FinalRefBoneInfo[i].ParentIndex,
                Position = originalSkeleton.ReferenceSkeleton.FinalRefBonePose[i].Translation,
                Orientation = originalSkeleton.ReferenceSkeleton.FinalRefBonePose[i].Rotation,
            };

            // if (i >= 1) // fix skeleton; all bones but 0
            //     skeletalMeshBone.Orientation.Conjugate();

            bones.Add(skeletalMeshBone);
            box.Min = skeletalMeshBone.Position.ComponentMin(box.Min);
            box.Max = skeletalMeshBone.Position.ComponentMax(box.Max);
        }
        return true;
    }

    public static bool TryConvert(this USplineMeshComponent? spline, out CStaticMesh convertedMesh, ENaniteMeshFormat naniteFormat = ENaniteMeshFormat.OnlyNormalLODs)
    {
        var originalMesh = spline?.GetStaticMesh().Load<UStaticMesh>();
        if (originalMesh == null)
        {
            convertedMesh = new CStaticMesh();
            return false;
        }
        return TryConvert(originalMesh, spline, out convertedMesh, naniteFormat);
    }

    public static bool TryConvert(this UStaticMesh originalMesh, out CStaticMesh convertedMesh, ENaniteMeshFormat naniteFormat = ENaniteMeshFormat.OnlyNormalLODs)
    {
        return TryConvert(originalMesh, null, out convertedMesh, naniteFormat);
    }

    public static bool TryConvert(this UStaticMesh originalMesh, USplineMeshComponent? spline,
        out CStaticMesh convertedMesh, ENaniteMeshFormat naniteFormat = ENaniteMeshFormat.OnlyNormalLODs)
    {
        convertedMesh = new CStaticMesh();
        if (originalMesh.RenderData?.Bounds == null || originalMesh.RenderData?.LODs is null)
            return false;

        convertedMesh.BoundingSphere = new FSphere(0f, 0f, 0f, originalMesh.RenderData.Bounds.SphereRadius / 2);
        convertedMesh.BoundingBox = new FBox(
            originalMesh.RenderData.Bounds.Origin - originalMesh.RenderData.Bounds.BoxExtent,
            originalMesh.RenderData.Bounds.Origin + originalMesh.RenderData.Bounds.BoxExtent);

        for (var i = 0; i < originalMesh.RenderData.LODs.Length; i++)
        {
            var srcLod = originalMesh.RenderData.LODs[i];
            if (srcLod.SkipLod) continue;

            var numTexCoords = srcLod.VertexBuffer!.NumTexCoords;
            var numVerts = srcLod.PositionVertexBuffer!.Verts.Length;
            if (numVerts == 0 && numTexCoords == 0)
            {
                continue;
            }

            if (numTexCoords > Constants.MAX_MESH_UV_SETS)
                throw new ParserException($"Static mesh has too many UV sets ({numTexCoords})");

            var screenSize = 0.0f;
            if (i < originalMesh.RenderData.ScreenSize.Length)
            {
                screenSize = originalMesh.RenderData.ScreenSize[i];
            }

            var staticMeshLod = new CStaticMeshLod
            {
                NumTexCoords = numTexCoords,
                ScreenSize = screenSize,
                HasNormals = true,
                HasTangents = true,
                IsTwoSided = srcLod.CardRepresentationData?.bMostlyTwoSided ?? false,
                Indices = new Lazy<uint[]>(() =>
                {
                    if (srcLod.IndexBuffer?.Buffer == null)
                        throw new ParserException("Static mesh LOD has no index buffer");
                    
                    var copy = new uint[srcLod.IndexBuffer.Buffer.Length];
                    Array.Copy(srcLod.IndexBuffer.Buffer, copy, copy.Length);
                    return copy;
                }),
                Sections = new Lazy<CMeshSection[]>(() =>
                {
                    var sections = new CMeshSection[srcLod.Sections.Length];
                    for (var j = 0; j < sections.Length; j++)
                    {
                        int materialIndex = srcLod.Sections[j].MaterialIndex;
                        while (materialIndex >= originalMesh.Materials.Length)
                        {
                            materialIndex--;
                        }

                        if (materialIndex < 0) sections[j] = new CMeshSection(srcLod.Sections[j]);
                        else
                        {
                            sections[j] = new CMeshSection(materialIndex, srcLod.Sections[j],
                                originalMesh.StaticMaterials?[materialIndex].MaterialSlotName.Text, // materialName
                                originalMesh.Materials[materialIndex]); // numFaces
                        }
                    }
                    return sections;
                })
            };

            staticMeshLod.AllocateVerts(numVerts);
            if (srcLod.ColorVertexBuffer!.NumVertices != 0)
                staticMeshLod.AllocateVertexColorBuffer();

            for (var j = 0; j < numVerts; j++)
            {
                var suv = srcLod.VertexBuffer.UV[j];
                if (suv.Normal[1].Data != 0)
                    throw new ParserException("Not implemented: should only be used in UE3");

                var pos = srcLod.PositionVertexBuffer.Verts[j];
                if (spline != null) // TODO normals
                {
                    var distanceAlong = USplineMeshComponent.GetAxisValueRef(ref pos, spline.ForwardAxis);
                    var sliceTransform = spline.CalcSliceTransform(distanceAlong);
                    USplineMeshComponent.SetAxisValueRef(ref pos, spline.ForwardAxis, 0f);
                    pos = sliceTransform.TransformPosition(pos);
                }

                staticMeshLod.Verts[j].Position = pos;
                UnpackNormals(suv.Normal, staticMeshLod.Verts[j]);
                staticMeshLod.Verts[j].UV.U = suv.UV[0].U;
                staticMeshLod.Verts[j].UV.V = suv.UV[0].V;

                for (var k = 1; k < numTexCoords; k++)
                {
                    staticMeshLod.ExtraUV.Value[k - 1][j].U = suv.UV[k].U;
                    staticMeshLod.ExtraUV.Value[k - 1][j].V = suv.UV[k].V;
                }

                if (srcLod.ColorVertexBuffer.NumVertices != 0)
                    staticMeshLod.VertexColors![j] = srcLod.ColorVertexBuffer.Data[j];
            }

            convertedMesh.LODs.Add(staticMeshLod);
        }

        if (naniteFormat != ENaniteMeshFormat.OnlyNormalLODs && TryConvertNaniteMesh(originalMesh, out CStaticMeshLod? naniteMesh) && naniteMesh is not null)
        {
            switch (naniteFormat)
            {
                case ENaniteMeshFormat.OnlyNaniteLOD:
                    foreach (var lod in convertedMesh.LODs) lod.Dispose();
                    convertedMesh.LODs.Clear();
                    convertedMesh.LODs.Add(naniteMesh);
                    break;
                case ENaniteMeshFormat.AllLayersNaniteFirst:
                    convertedMesh.LODs.Insert(0, naniteMesh);
                    break;
                case ENaniteMeshFormat.AllLayersNaniteLast:
                    convertedMesh.LODs.Add(naniteMesh);
                    break;
            }
        }

        convertedMesh.FinalizeMesh();
        return true;
    }

    private static bool TryConvertNaniteMesh(UStaticMesh originalMesh, out CStaticMeshLod? staticMeshLod)
    {
        FNaniteResources? nanite = originalMesh.RenderData?.NaniteResources;
        if (nanite is null || nanite.PageStreamingStates.Length <= 0)
        {
            staticMeshLod = null;
            return false;
        }

        nanite.LoadAllPages();

        // Identify all high quality clusters
        IEnumerable<FCluster> goodClusters = nanite.LoadedPages.Where(p => p != null)
                .SelectMany(p => p!.Clusters)
                .Where(x => x.EdgeLength < 0.0f);

        // Check if we even have tris to parse.
        long numTris = 0;
        long numVerts = 0;
        var bHasTangents = false;
        uint numUVs = 0;
        foreach (var cluster in goodClusters)
        {
            numTris += cluster.TriIndices.Length;
            numVerts += cluster.Vertices.Length;
            bHasTangents &= cluster.bHasTangents;
            numUVs = Math.Max(numUVs, cluster.NumUVs);
        }

        // Identify the max number of UVs
        int numTexCoords = nanite.Archive.Game >= EGame.GAME_UE5_6 ? (int)numUVs : nanite.NumInputTexCoords;
        if (numTris <= 0 || numVerts <= 0)
        {
            staticMeshLod = null;
            return false;
        }

        // Identify number of faces per materials.
        CMeshSection[] matSections = new CMeshSection[originalMesh.Materials.Length];
        for (int materialIndex = 0; materialIndex < originalMesh.Materials.Length; materialIndex++)
        {
            matSections[materialIndex] = new CMeshSection(
                materialIndex,
                0,
                0,
                originalMesh.StaticMaterials?[materialIndex].MaterialSlotName.Text,
                originalMesh.Materials[materialIndex]
            );
        }
        Parallel.ForEach(goodClusters, (c) =>
        {
            if (c.ShouldUseMaterialTable())
            {
                foreach (FMaterialRange range in c.MaterialRanges)
                {
                    Interlocked.Add(ref matSections[range.MaterialIndex].NumFaces, (int) range.TriLength);
                }
            }
            else
            {
                Interlocked.Add(ref matSections[c.Material0Index].NumFaces, (int) c.Material0Length);
                Interlocked.Add(ref matSections[c.Material1Index].NumFaces, (int) c.Material1Length);
                Interlocked.Add(ref matSections[c.Material2Index].NumFaces, (int) (c.NumTris - (c.Material1Length + c.Material0Length)));
            }
        });

        // Create trackers for write positions for each material since we are working in parallel.
        int[] triBufferWriteOffsets = new int[matSections.Length];
        for (int i = 1; i < matSections.Length; i++)
        {
            matSections[i].FirstIndex = triBufferWriteOffsets[i] = matSections[i - 1].FirstIndex + matSections[i - 1].NumFaces;
        }

        // Create the return object
        uint[] triBuffer = new uint[(matSections[^1].FirstIndex + matSections[^1].NumFaces) * 3];
        var outMesh = new CStaticMeshLod
        {
            NumTexCoords = numTexCoords,
            HasNormals = true,
            HasTangents = bHasTangents,
            IsTwoSided = true,
            Indices = new Lazy<uint[]>(triBuffer),
            Sections = new Lazy<CMeshSection[]>(matSections)
        };
        outMesh.AllocateVerts((int) numVerts);
        outMesh.AllocateVertexColorBuffer();

        // Write vertices and the tri buffer

        uint vertOffsetTracker = 0;
        Parallel.ForEach(goodClusters, (c) =>
        {
            uint globalVertOffset = Interlocked.Add(ref vertOffsetTracker, c.NumVerts) - c.NumVerts;
            // Write the vertices
            uint clusterVertOffset = 0;
            foreach (var vert in c.Vertices)
            {
                uint vertOffset = globalVertOffset + clusterVertOffset++;
                outMesh.Verts[vertOffset].Position = vert!.Pos;
                outMesh.Verts[vertOffset].Normal = new FVector4(vert.Attributes!.Normal.X, vert.Attributes!.Normal.Y, vert.Attributes!.Normal.Z, 0);
                if (outMesh.HasTangents)
                {
                    outMesh.Verts[vertOffset].Tangent = vert.Attributes.TangentXAndSign;
                }

                outMesh.Verts[vertOffset].UV.U = vert.Attributes.UVs[0].X;
                outMesh.Verts[vertOffset].UV.V = vert.Attributes.UVs[0].Y;

                for (int texCoordIndex = 1; texCoordIndex < numTexCoords; texCoordIndex++)
                {
                    outMesh.ExtraUV.Value[texCoordIndex - 1][vertOffset].U = vert.Attributes.UVs[texCoordIndex].X;
                    outMesh.ExtraUV.Value[texCoordIndex - 1][vertOffset].V = vert.Attributes.UVs[texCoordIndex].Y;
                }

                outMesh.VertexColors![vertOffset] = vert.Attributes.Color;
            }
            void writeToTriBuffer(uint matIndex, uint triStart, uint triLength)
            {
                // Abort early if no values
                if (triLength <= 0)
                {
                    return;
                }
                long triBufferWriteOffset = Interlocked.Add(ref triBufferWriteOffsets[matIndex], (int) triLength) - triLength;
                triBufferWriteOffset *= 3;
                for (long localTriIndex = 0; localTriIndex < triLength; localTriIndex++)
                {
                    triBuffer[triBufferWriteOffset++] = c.TriIndices[triStart + localTriIndex].X + globalVertOffset;
                    triBuffer[triBufferWriteOffset++] = c.TriIndices[triStart + localTriIndex].Y + globalVertOffset;
                    triBuffer[triBufferWriteOffset++] = c.TriIndices[triStart + localTriIndex].Z + globalVertOffset;
                }
            }
            // Write the tris
            if (c.ShouldUseMaterialTable())
            {
                foreach (FMaterialRange range in c.MaterialRanges)
                {
                    writeToTriBuffer(range.MaterialIndex, range.TriStart, range.TriLength);
                }
            }
            else
            {
                writeToTriBuffer(c.Material0Index, 0, c.Material0Length);
                writeToTriBuffer(c.Material1Index, c.Material0Length, c.Material1Length);
                writeToTriBuffer(c.Material2Index, c.Material0Length + c.Material1Length, c.NumTris - (c.Material0Length + c.Material1Length));
            }
        });

        // It's just easier to math out this way
        foreach (CMeshSection s in matSections)
        {
            s.FirstIndex *= 3;
        }

        // aggressively garbage collect since the asset is re-parsed every time by FModel
        // we don't need most of this data to still exist post mesh export anyway.
        // we also don't want that to json serialize anyway since 400mb+ json files are no fun.
        for (int i = 0; i < nanite.LoadedPages.Length; i++)
        {
            nanite.LoadedPages[i] = null;
        }
        GC.Collect();

        outMesh.IsNanite = true;
        staticMeshLod = outMesh;
        return true;
    }

    public static bool TryConvert(this USkeletalMesh originalMesh, out CSkeletalMesh convertedMesh)
    {
        convertedMesh = new CSkeletalMesh();
        if (originalMesh.LODModels == null) return false;

        convertedMesh.BoundingSphere = new FSphere(0f, 0f, 0f, originalMesh.ImportedBounds.SphereRadius / 2);
        convertedMesh.BoundingBox = new FBox(
            originalMesh.ImportedBounds.Origin - originalMesh.ImportedBounds.BoxExtent,
            originalMesh.ImportedBounds.Origin + originalMesh.ImportedBounds.BoxExtent);

        for (var i = 0; i < originalMesh.LODModels.Length; i++)
        {
            var srcLod = originalMesh.LODModels[i];
            if (srcLod.SkipLod) continue;

            var numTexCoords = srcLod.NumTexCoords;
            if (numTexCoords > Constants.MAX_MESH_UV_SETS)
                throw new ParserException($"Skeletal mesh has too many UV sets ({numTexCoords})");

            var skeletalMeshLod = new CSkelMeshLod
            {
                NumTexCoords = numTexCoords,
                ScreenSize = originalMesh.LODInfo[i].ScreenSize.Default,
                HasNormals = true,
                HasTangents = true,
                Indices = new Lazy<uint[]>(() =>
                {
                    if (srcLod.Indices?.Buffer == null)
                        throw new ParserException("Skeletal mesh LOD has no index buffer");
                    
                    var copy = new uint[srcLod.Indices.Buffer.Length];
                    Array.Copy(srcLod.Indices.Buffer, copy, copy.Length);
                    return copy;
                }),
                Sections = new Lazy<CMeshSection[]>(() =>
                {
                    var sections = new CMeshSection[srcLod.Sections.Length];
                    for (var j = 0; j < sections.Length; j++)
                    {
                        int materialIndex = srcLod.Sections[j].MaterialIndex;
                        if (materialIndex < 0) // UE4 using Clamp(0, Materials.Num()), not Materials.Num()-1
                        {
                            materialIndex = 0;
                        }
                        else while (materialIndex >= originalMesh.Materials?.Length)
                        {
                            materialIndex--;
                        }

                        if (materialIndex < 0) sections[j] = new CMeshSection(srcLod.Sections[j]);
                        else
                        {
                            sections[j] = new CMeshSection(materialIndex, srcLod.Sections[j],
                                originalMesh.SkeletalMaterials[materialIndex].MaterialSlotName.Text,
                                originalMesh.SkeletalMaterials[materialIndex].Material);
                        }
                    }

                    return sections;
                })
            };

            var bUseVerticesFromSections = false;
            var vertexCount = srcLod.VertexBufferGPUSkin.GetVertexCount();
            if (vertexCount == 0 && srcLod.Sections.Length > 0 && srcLod.Sections[0].SoftVertices.Length > 0)
            {
                bUseVerticesFromSections = true;
                foreach (var section in srcLod.Sections)
                {
                    vertexCount += section.SoftVertices.Length;
                }
            }

            skeletalMeshLod.AllocateVerts(vertexCount);

            var chunkIndex = -1;
            var chunkVertexIndex = 0;
            long lastChunkVertex = -1;
            ushort[]? boneMap = null;
            var vertBuffer = srcLod.VertexBufferGPUSkin;

            if (srcLod.ColorVertexBuffer.Data.Length == vertexCount)
                skeletalMeshLod.AllocateVertexColorBuffer();

            for (var vert = 0; vert < vertexCount; vert++)
            {
                while (vert >= lastChunkVertex) // this will fix any issues with empty chunks or sections
                {
                    if (srcLod.Chunks.Length > 0) // proceed to next chunk or section
                    {
                        // pre-UE4.13 code: chunks
                        var c = srcLod.Chunks[++chunkIndex];
                        lastChunkVertex = c.BaseVertexIndex + c.NumRigidVertices + c.NumSoftVertices;
                        boneMap = c.BoneMap;
                    }
                    else
                    {
                        // UE4.13+ code: chunk information migrated to sections
                        var s = srcLod.Sections[++chunkIndex];
                        lastChunkVertex = s.BaseVertexIndex + s.NumVertices;
                        boneMap = s.BoneMap;
                    }

                    chunkVertexIndex = 0;
                }

                FSkelMeshVertexBase v; // has everything but UV[]
                if (bUseVerticesFromSections)
                {
                    var v0 = srcLod.Sections[chunkIndex].SoftVertices[chunkVertexIndex++];
                    v = v0;

                    skeletalMeshLod.Verts[vert].UV = v0.UV[0]; // UV: simply copy float data
                    for (var texCoordIndex = 1; texCoordIndex < numTexCoords; texCoordIndex++)
                    {
                        skeletalMeshLod.ExtraUV.Value[texCoordIndex - 1][vert] = v0.UV[texCoordIndex];
                    }
                }
                else if (!vertBuffer.bUseFullPrecisionUVs)
                {
                    var v0 = vertBuffer.VertsHalf[vert];
                    v = v0;

                    skeletalMeshLod.Verts[vert].UV = (FMeshUVFloat) v0.UV[0]; // UV: convert half -> float
                    for (var texCoordIndex = 1; texCoordIndex < numTexCoords; texCoordIndex++)
                    {
                        skeletalMeshLod.ExtraUV.Value[texCoordIndex - 1][vert] = (FMeshUVFloat) v0.UV[texCoordIndex];
                    }
                }
                else
                {
                    var v0 = vertBuffer.VertsFloat[vert];
                    v = v0;

                    skeletalMeshLod.Verts[vert].UV = v0.UV[0]; // UV: simply copy float data
                    for (var texCoordIndex = 1; texCoordIndex < numTexCoords; texCoordIndex++)
                    {
                        skeletalMeshLod.ExtraUV.Value[texCoordIndex - 1][vert] = v0.UV[texCoordIndex];
                    }
                }

                skeletalMeshLod.Verts[vert].Position = v.Pos;
                UnpackNormals(v.Normal, skeletalMeshLod.Verts[vert]);
                if (skeletalMeshLod.VertexColors != null)
                {
                    skeletalMeshLod.VertexColors[vert] = srcLod.ColorVertexBuffer.Data[vert];
                }

                var scale = v.Infs.bUse16BitBoneWeight ? Constants.UShort_Bone_Scale : Constants.Byte_Bone_Scale;
                foreach (var (weight, boneIndex) in v.Infs.BoneWeight.Zip(v.Infs.BoneIndex))
                {
                    if (weight != 0)
                    {
                        var bone = boneMap[boneIndex];
                        skeletalMeshLod.Verts[vert].AddInfluence(bone, weight, weight * scale);
                    }
                }
            }

            convertedMesh.LODs.Add(skeletalMeshLod);
        }

        for (var i = 0; i < originalMesh.ReferenceSkeleton.FinalRefBoneInfo.Length; i++)
        {
            var skeletalMeshBone = new CSkelMeshBone
            {
                Name = originalMesh.ReferenceSkeleton.FinalRefBoneInfo[i].Name,
                ParentIndex = originalMesh.ReferenceSkeleton.FinalRefBoneInfo[i].ParentIndex,
                Position = originalMesh.ReferenceSkeleton.FinalRefBonePose[i].Translation,
                Orientation = originalMesh.ReferenceSkeleton.FinalRefBonePose[i].Rotation
            };

            // if (i >= 1) // fix skeleton; all bones but 0
            //     skeletalMeshBone.Orientation.Conjugate();

            convertedMesh.RefSkeleton.Add(skeletalMeshBone);
        }

        convertedMesh.FinalizeMesh();
        return true;
    }

    public struct FSectionRange
    {
        public int End;
        public int Index;
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

    public static bool TryConvert(this ALandscapeProxy landscape, ULandscapeComponent[]? landscapeComponents, ELandscapeExportFlags flags, out CStaticMesh? convertedMesh, out Dictionary<string,Image> heightMaps, out Dictionary<string, SKBitmap> weightMaps)
    {
        heightMaps = [];
        weightMaps = [];
        convertedMesh = null;

        if (landscapeComponents == null)
        {
            var comps = landscape.LandscapeComponents;
            landscapeComponents = new ULandscapeComponent[comps.Length];
            for (var i = 0; i < comps.Length; i++)
                landscapeComponents[i] = comps[i].Load<ULandscapeComponent>()!;
        }

        var componentSize = landscape.ComponentSizeQuads;

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var comp in landscapeComponents)
        {
            if (componentSize == -1)
                componentSize = comp.ComponentSizeQuads;
            else
            {
                Debug.Assert(componentSize == comp.ComponentSizeQuads);
            }

            comp.GetComponentExtent(ref minX, ref minY, ref maxX, ref maxY);
        }

        // Create and fill in the vertex position data source.
        int componentSizeQuads = ((componentSize + 1) >> 0 /*Landscape->ExportLOD*/) - 1;
        float scaleFactor = (float)componentSizeQuads / componentSize;
        int numComponents = landscapeComponents.Length;
        int vertexCountPerComponent = (componentSizeQuads + 1) * (componentSizeQuads + 1);
        int vertexCount = numComponents * vertexCountPerComponent;
        int triangleCount = numComponents * (componentSizeQuads * componentSizeQuads) * 2;

        FVector2D uvScale = new FVector2D(1.0f, 1.0f) / new FVector2D((maxX - minX) + 1, (maxY - minY) + 1);

        // For image export
        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        CStaticMeshLod? landscapeLod = null;
        if (flags.HasFlag(ELandscapeExportFlags.Mesh))
        {
            landscapeLod = new CStaticMeshLod();
            landscapeLod.NumTexCoords = 2; // TextureUV and weightmapUV
            landscapeLod.AllocateVerts(vertexCount);
            landscapeLod.AllocateVertexColorBuffer();
        }

        var extraVertexColorMap = new ConcurrentDictionary<string, CVertexColor>();

        var weightMapsInternal = new ConcurrentDictionary<string, SKBitmap>();
        var weightMapsPixels = new ConcurrentDictionary<int, IntPtr>();
        var weightMapLock = new object();
        var heightMapData = new L16[height * width];

        // https://github.com/EpicGames/UnrealEngine/blob/5de4acb1f05e289620e0a66308ebe959a4d63468/Engine/Source/Editor/UnrealEd/Private/Fbx/FbxMainExport.cpp#L4549
        var tasks = new Task[landscapeComponents.Length];
        for (int i = 0, selectedComponentIndex = 0; i < landscapeComponents.Length; i++)
        {
            var comp = landscapeComponents[i];

            var CDI = new FLandscapeComponentDataInterface(comp, 0);
            CDI.EnsureWeightmapTextureDataCache();

            int baseVertIndex = selectedComponentIndex++ * vertexCountPerComponent;

            var weightMapAllocs = comp.GetWeightmapLayerAllocations();

            var compTransform = comp.GetComponentTransform();
            var relLoc = comp.GetRelativeLocation();

            var task = Task.Run(() =>
            {
                for (int vertIndex = 0; vertIndex < vertexCountPerComponent; vertIndex++)
                {
                    CDI.VertexIndexToXY(vertIndex, out var vertX, out var vertY);

                    var vertCoord = CDI.GetLocalVertex(vertX, vertY);
                    var position = vertCoord + relLoc;

                    CDI.GetLocalTangentVectors(vertIndex, out var tangentX, out var biNormal, out var normal);

                    normal /= compTransform.Scale3D;
                    normal.Normalize();
                    FVector4.AsFVector(ref tangentX) /= compTransform.Scale3D;
                    FVector4.AsFVector(ref tangentX).Normalize();
                    biNormal /= compTransform.Scale3D;
                    biNormal.Normalize();

                    var textureUv = new FVector2D(vertX * scaleFactor + comp.SectionBaseX,
                        vertY * scaleFactor + comp.SectionBaseY);
                    var textureUv2 = new TIntVector2<int>((int)textureUv.X - minX, (int)textureUv.Y - minY);

                    var weightmapUv = (textureUv - new FVector2D(minX, minY)) * uvScale;

                    heightMapData[textureUv2.X + textureUv2.Y * width] = new L16((ushort)(CDI.GetVertex(vertX, vertY) + relLoc).Z);

                    foreach (var allocationInfo in weightMapAllocs)
                    {
                        var weight = CDI.GetLayerWeight(vertX, vertY, allocationInfo);
                        if (weight == 0) continue;

                        var layerName = allocationInfo.GetLayerName();

                        // weight as Mesh Vertex colors
                        if ((flags & ELandscapeExportFlags.Mesh) == ELandscapeExportFlags.Mesh)
                        {
                            // ReSharper disable once CanSimplifyDictionaryLookupWithTryAdd
                            if (!extraVertexColorMap.ContainsKey(layerName))
                            {
                                var shortName = layerName.SubstringBefore("_LayerInfo");
                                shortName = shortName.Substring(0, Math.Min(20 - 6, shortName.Length));
                                extraVertexColorMap.TryAdd(layerName, new CVertexColor(shortName, new FColor[vertexCount]));
                            }

                            extraVertexColorMap[layerName].ColorData[baseVertIndex + vertIndex] = new FColor(weight, weight, weight, weight);
                        }

                        var pixelX = textureUv2.X;
                        var pixelY = textureUv2.Y;

                        if ((flags & ELandscapeExportFlags.Weightmap) == ELandscapeExportFlags.Weightmap)
                        {
                            lock (weightMapLock)
                            {
                                if (!weightMapsInternal.ContainsKey(layerName))
                                {
                                    var bitmap = new SKBitmap(width, height, SKColorType.Gray8, SKAlphaType.Unpremul);
                                    weightMapsInternal.TryAdd(layerName, bitmap);
                                    weightMapsPixels.TryAdd(allocationInfo.GetLayerNameHash(), bitmap.GetPixels());
                                }
                            }

                            // weightMaps[layerName].SetPixel((int)pixelX, (int)pixelY, new SKColor(weight, weight, weight, 255)); // slow
                            unsafe
                            {
                                var pixels =
                                    (byte*)weightMapsPixels[
                                        allocationInfo
                                            .GetLayerNameHash()]; // possible race condition but it doesn't matter
                                pixels[pixelY * width + pixelX] = weight;
                            }

                            // debug color
                            // var infoObject = allocationInfo.LayerInfo.Load<ULandscapeLayerInfoObject>();
                            // var cl = infoObject.LayerUsageDebugColor.ToFColor(true);
                            // weightMapsData[allocationInfo.LayerInfo.Name].SetPixel((int)pixel_x, (int)pixel_y, new SKColor(cl.R, cl.G, cl.B, weight));
                        }
                    }

                    if ((flags & ELandscapeExportFlags.Mesh) == ELandscapeExportFlags.Mesh && landscapeLod != null)
                    {
                        var vert = landscapeLod.Verts[baseVertIndex + vertIndex];
                        vert.Position = position;
                        vert.Normal = new FVector4(normal); // this might be broken
                        vert.Tangent = tangentX;
                        vert.UV = (FMeshUVFloat)textureUv;

                        landscapeLod.ExtraUV.Value[0][baseVertIndex + vertIndex] = (FMeshUVFloat)weightmapUv;
                    }

                    if (!weightMapsInternal.ContainsKey("NormalMap_DX")) // directX normal map
                        weightMapsInternal["NormalMap_DX"] = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
                    var normaBitmap = weightMapsInternal["NormalMap_DX"];
                    unsafe
                    {
                        var pixels = (byte*)normaBitmap.GetPixels();
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
            tasks[i] = task;
        }

        Task.WaitAll(tasks);

        // image.Save(File.OpenWrite("heightmap.png"), new PngEncoder());
        if (flags.HasFlag(ELandscapeExportFlags.Heightmap))
        {
            var image = Image.LoadPixelData<L16>(heightMapData, width, height);
            heightMaps.Add("heightmap", image);
        }

        // skimage
        //var heightMap = new SKBitmap(Width, Height, SKColorType.RgbaF16, SKAlphaType.Unpremul);
        // heightMap.Encode(SKEncodedImageFormat.Png, 100).SaveTo(File.OpenWrite("heightmap.png"));
        if (flags.HasFlag(ELandscapeExportFlags.Weightmap))
        {
            weightMaps = weightMapsInternal.ToDictionary(x => x.Key, x => x.Value);
        }

        if (!flags.HasFlag(ELandscapeExportFlags.Mesh) || landscapeLod == null)
        {
            return true;
        }

        landscapeLod.ExtraVertexColors = extraVertexColorMap.Values.ToArray();
        extraVertexColorMap.Clear();
        var landscapeMaterial = landscape.LandscapeMaterial;
        var mat = landscapeMaterial.Load<UMaterialInterface>();
        landscapeLod.Sections = new Lazy<CMeshSection[]>(new[]
        {
            new CMeshSection(0, 0, triangleCount, mat?.Name ?? "DefaultMaterial", landscapeMaterial.ResolvedObject)
        });

        landscapeLod.Indices = new Lazy<uint[]>(() =>
        {
            var meshIndices = new List<uint>(triangleCount * 3); // TODO: replace with ArrayPool.Shared.Rent
            // https://github.com/EpicGames/UnrealEngine/blob/5de4acb1f05e289620e0a66308ebe959a4d63468/Engine/Source/Editor/UnrealEd/Private/Fbx/FbxMainExport.cpp#L4657
            for (int componentIndex = 0; componentIndex < numComponents; componentIndex++)
            {
                int baseVertIndex = componentIndex * vertexCountPerComponent;

                for (int Y = 0; Y < componentSizeQuads; Y++)
                {
                    for (int X = 0; X < componentSizeQuads; X++)
                    {
                        if (true) // (VisibilityData[BaseVertIndex + Y * (ComponentSizeQuads + 1) + X] < VisThreshold)
                        {
                            var w1 = baseVertIndex + (X + 0) + (Y + 0) * (componentSizeQuads + 1);
                            var w2 = baseVertIndex + (X + 1) + (Y + 1) * (componentSizeQuads + 1);
                            var w3 = baseVertIndex + (X + 1) + (Y + 0) * (componentSizeQuads + 1);

                            meshIndices.Add((uint)w1);
                            meshIndices.Add((uint)w2);
                            meshIndices.Add((uint)w3);

                            var w4 = baseVertIndex + (X + 0) + (Y + 0) * (componentSizeQuads + 1);
                            var w5 = baseVertIndex + (X + 0) + (Y + 1) * (componentSizeQuads + 1);
                            var w6 = baseVertIndex + (X + 1) + (Y + 1) * (componentSizeQuads + 1);

                            meshIndices.Add((uint)w4);
                            meshIndices.Add((uint)w5);
                            meshIndices.Add((uint)w6);
                        }
                    }
                }
            }
            return meshIndices.ToArray();
        });

        convertedMesh = new CStaticMesh();

        FVector min = new(minX, minY, 0);
        FVector max = new(maxX + 1, maxY + 1, Math.Max(maxX - minX, maxY - minY));

        convertedMesh.BoundingBox = new FBox(min, max);

        FVector extent = (max - min) * 0.5f;
        convertedMesh.BoundingSphere = new FSphere(0f, 0f, 0f, extent.Size());

        convertedMesh.LODs.Add(landscapeLod);
        return true;
    }
}