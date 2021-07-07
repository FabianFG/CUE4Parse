using System;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse_Conversion.Meshes.PSK;
using Serilog;

namespace CUE4Parse_Conversion.Meshes
{
    public static class MeshConverter
    {
        private const int _MAX_MESH_UV_SETS = 8;
        
        public static bool TryConvert(this UStaticMesh originalMesh, out CStaticMesh convertedMesh)
        {
            convertedMesh = new CStaticMesh();
            if (originalMesh.RenderData == null) return false;
            
            var numLods = originalMesh.RenderData.LODs.Length;
            convertedMesh.LODs = new CStaticMeshLod[numLods];
            for (var i = 0; i < convertedMesh.LODs.Length; i++)
            {
                if (originalMesh.RenderData.LODs[i] is not
                {
                    VertexBuffer: not null,
                    PositionVertexBuffer: not null,
                    ColorVertexBuffer: not null,
                    IndexBuffer: not null
                } srcLod) continue;
                
                var numTexCoords = srcLod.VertexBuffer.NumTexCoords;
                var numVerts = srcLod.PositionVertexBuffer.Verts.Length;
                if (numVerts == 0 && numTexCoords == 0 && i < numLods - 1) {
                    Log.Logger.Debug($"LOD {i} is stripped, skipping...");
                    continue;
                }

                if (numTexCoords > _MAX_MESH_UV_SETS)
                    throw new ParserException($"Static mesh has too many UV sets ({numTexCoords})");

                convertedMesh.LODs[i] = new CStaticMeshLod
                {
                    NumTexCoords = numTexCoords,
                    HasNormals = true,
                    HasTangents = true,
                    Indices = new Lazy<FRawStaticIndexBuffer>(srcLod.IndexBuffer),
                    Sections = new Lazy<CMeshSection[]>(() =>
                    {
                        var sections = new CMeshSection[srcLod.Sections.Length];
                        for (var j = 0; j < sections.Length; j++)
                        {
                            sections[j] = new CMeshSection(originalMesh.Materials?[srcLod.Sections[j].MaterialIndex],
                                srcLod.Sections[j].FirstIndex, srcLod.Sections[j].NumTriangles);
                        }

                        return sections;
                    })
                };

                convertedMesh.LODs[i].AllocateVerts(numVerts);
                if (srcLod.ColorVertexBuffer.NumVertices != 0)
                    convertedMesh.LODs[i].AllocateVertexColorBuffer();

                for (var j = 0; j < numVerts; j++)
                {
                    var suv = srcLod.VertexBuffer.UV[j];
                    if (suv.Normal[1].Data != 0)
                        throw new ParserException("Not implemented: should only be used in UE3");
                    
                    convertedMesh.LODs[i].Verts[j].Position = srcLod.PositionVertexBuffer.Verts[j];
                    convertedMesh.LODs[i].Verts[j].Normal.Data = suv.Normal[2].Data;
                    convertedMesh.LODs[i].Verts[j].Tangent.Data = suv.Normal[0].Data;
                    convertedMesh.LODs[i].Verts[j].UV.U = suv.UV[0].U;
                    convertedMesh.LODs[i].Verts[j].UV.V = suv.UV[0].V;
                    
                    for (var k = 1; k < numTexCoords; k++)
                    {
                        convertedMesh.LODs[i].ExtraUV.Value[k - 1][j].U = suv.UV[k].U;
                        convertedMesh.LODs[i].ExtraUV.Value[k - 1][j].V = suv.UV[k].V;
                    }

                    if (srcLod.ColorVertexBuffer.NumVertices != 0)
                        convertedMesh.LODs[i].VertexColors[j] = srcLod.ColorVertexBuffer.Data[j];
                }
            }

            convertedMesh.FinalizeMesh();
            return true;
        }
        
        public static bool TryConvert(this USkeletalMesh originalMesh, out CSkeletalMesh convertedMesh)
        {
            convertedMesh = new CSkeletalMesh();
            if (originalMesh.LODModels == null) return false;

            var numLods = originalMesh.LODModels.Length;
            convertedMesh.LODs = new CSkelMeshLod[numLods];
            for (var i = 0; i < convertedMesh.LODs.Length; i++)
            {
                if (originalMesh.LODModels[i] is not { } srcLod) continue;
                
                if (srcLod.Indices.Indices16.Length == 0 && srcLod.Indices.Indices32.Length == 0)
                {
                    Log.Logger.Debug($"LOD {i} has no indices, skipping...");
                    continue;
                }
                
                var numTexCoords = srcLod.NumTexCoords;
                if (numTexCoords > _MAX_MESH_UV_SETS)
                    throw new ParserException($"Skeletal mesh has too many UV sets ({numTexCoords})");
                
                convertedMesh.LODs[i] = new CSkelMeshLod
                {
                    NumTexCoords = numTexCoords,
                    HasNormals = true,
                    HasTangents = true,
                };
                
                var bUseVerticesFromSections = false;
                var vertexCount = srcLod.VertexBufferGPUSkin.GetVertexCount();
                if (vertexCount == 0 && srcLod.Sections.Length > 0 && srcLod.Sections[0].SoftVertices.Length > 0)
                {
                    bUseVerticesFromSections = true;
                    for (var j = 0; j < srcLod.Sections.Length; j++)
                    {
                        vertexCount += srcLod.Sections[i].SoftVertices.Length;
                    }
                }
                
                convertedMesh.LODs[i].AllocateVerts(vertexCount);
                
                // https://github.com/gildor2/UEViewer/blob/master/Unreal/UnrealMesh/UnMesh4.cpp#L1981
            }
            
            convertedMesh.FinalizeMesh();
            return true;
        }
    }
}