using System;
using System.Collections.Generic;
using System.IO;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.Utils;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Meshes.Common;
using CUE4Parse_Conversion.Meshes.PSK;
using Serilog;

namespace CUE4Parse_Conversion.Meshes
{
    public class MeshExporter : ExporterBase
    {
        private const int _PSK_VERSION = 20100422;
        
        public readonly string MeshName;
        public readonly List<Mesh> MeshLods;
        
        public MeshExporter(UStaticMesh originalMesh, ELodFormat lodFormat = ELodFormat.FirstLod, bool exportMaterials = true)
        {
            MeshLods = new List<Mesh>();
            MeshName = originalMesh.Owner?.Name ?? originalMesh.Name;
            
            if (!originalMesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count < 1)
            {
                Log.Logger.Warning($"Mesh '{MeshName}' has no LODs");
                return;
            }

            var i = 0;
            foreach (var lod in convertedMesh.LODs)
            {
                if (lod.SkipLod)
                {
                    Log.Logger.Warning($"LOD {i} in mesh '{MeshName}' should be skipped");
                    continue;
                }
                
                using var writer = new FCustomArchiveWriter();
                var materialExports = exportMaterials ? new List<MaterialExporter>() : null;
                ExportStaticMeshLods(lod, writer, materialExports);
                
                MeshLods.Add(new Mesh($"{MeshName}_LOD{i}.pskx", writer.GetBuffer(), materialExports ?? new List<MaterialExporter>()));
                if (lodFormat == ELodFormat.FirstLod) break;
                i++;
            }
        }

        public MeshExporter(USkeletalMesh originalMesh, ELodFormat lodFormat = ELodFormat.FirstLod, bool exportMaterials = true)
        {
            MeshLods = new List<Mesh>();
            MeshName = originalMesh.Owner?.Name ?? originalMesh.Name;
            
            if (!originalMesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count < 1)
            {
                Log.Logger.Warning($"Mesh '{MeshName}' has no LODs");
                return;
            }
            
            var i = 0;
            foreach (var lod in convertedMesh.LODs)
            {
                if (lod.SkipLod)
                {
                    Log.Logger.Warning($"LOD {i} in mesh '{MeshName}' should be skipped");
                    continue;
                }
                
                var usePskx = convertedMesh.LODs[i].NumVerts > 65536;
                using var writer = new FCustomArchiveWriter();
                var materialExports = exportMaterials ? new List<MaterialExporter>() : null;
                ExportSkeletalMeshLod(lod, convertedMesh.RefSkeleton, writer, materialExports);
                
                MeshLods.Add(new Mesh($"{MeshName}_LOD{i}.psk{(usePskx ? 'x' : "")}", writer.GetBuffer(), materialExports ?? new List<MaterialExporter>()));
                if (lodFormat == ELodFormat.FirstLod) break;
                i++;
            }
        }

        private void ExportStaticMeshLods(CStaticMeshLod lod, FCustomArchiveWriter writer, List<MaterialExporter>? materialExports)
        {
            var share = new CVertexShare();
            var boneHdr = new VChunkHeader();
            var infHdr = new VChunkHeader();
            
            share.Prepare(lod.Verts);
            foreach (var vert in lod.Verts)
            {
                share.AddVertex(vert.Position, vert.Normal);
            }

            ExportCommonMeshData(writer, lod.Sections.Value, lod.Verts, lod.Indices.Value, share, materialExports);

            boneHdr.DataCount = 0;
            boneHdr.DataSize = 120;
            writer.SerializeChunkHeader(boneHdr, "REFSKELT");

            infHdr.DataCount = 0;
            infHdr.DataSize = 12;
            writer.SerializeChunkHeader(infHdr, "RAWWEIGHTS");

            ExportVertexColors(writer, lod.VertexColors, lod.NumVerts);
            ExportExtraUV(writer, lod.ExtraUV.Value, lod.NumVerts, lod.NumTexCoords);
        }

        private void ExportSkeletalMeshLod(CSkelMeshLod lod, List<CSkelMeshBone> bones, FCustomArchiveWriter writer, List<MaterialExporter>? materialExports)
        {
            var share = new CVertexShare();
            var boneHdr = new VChunkHeader();
            var infHdr = new VChunkHeader();
            
            share.Prepare(lod.Verts);
            foreach (var vert in lod.Verts)
            {
                var weightsHash = vert.PackedWeights;
                for (var i = 0; i < vert.Bone.Length; i++)
                {
                    weightsHash ^= (uint)vert.Bone[i] << i;
                }
                
                share.AddVertex(vert.Position, vert.Normal, weightsHash);
            }
            
            ExportCommonMeshData(writer, lod.Sections.Value, lod.Verts, lod.Indices.Value, share, materialExports);

            var numBones = bones.Count;
            boneHdr.DataCount = numBones;
            boneHdr.DataSize = 120;
            writer.SerializeChunkHeader(boneHdr, "REFSKELT");
            for (var i = 0; i < numBones; i++)
            {
                var numChildren = 0;
                for (var j = 0; j < numBones; j++)
                    if (j != i && bones[j].ParentIndex == i)
                        numChildren++;
                
                var bone = new VBone
                {
                    Name = bones[i].Name.Text,
                    NumChildren = numChildren,
                    ParentIndex = bones[i].ParentIndex,
                    BonePos = new VJointPosPsk
                    {
                        Position = bones[i].Position,
                        Orientation = bones[i].Orientation
                    }
                };

                // MIRROR_MESH
                bone.BonePos.Orientation.Y *= -1;
                bone.BonePos.Orientation.W *= -1;
                bone.BonePos.Position.Y *= -1;
                
                bone.Serialize(writer);
            }
            
            var numInfluences = 0;
            for (var i = 0; i < share.Points.Count; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    if (lod.Verts[share.VertToWedge.Value[i]].Bone[j] < 0)
                        break;
                    numInfluences++;
                }
            }
            infHdr.DataCount = numInfluences;
            infHdr.DataSize = 12;
            writer.SerializeChunkHeader(infHdr, "RAWWEIGHTS");
            for (var i = 0; i < share.Points.Count; i++)
            {
                var v = lod.Verts[share.VertToWedge.Value[i]];
                var unpackedWeights = v.UnpackWeights();
                
                for (var j = 0; j < 4; j++)
                {
                    if (v.Bone[j] < 0)
                        break;

                    writer.Write(unpackedWeights[j]);
                    writer.Write(i);
                    writer.Write((int)v.Bone[j]);
                }
            }

            ExportVertexColors(writer, lod.VertexColors, lod.NumVerts);
            ExportExtraUV(writer, lod.ExtraUV.Value, lod.NumVerts, lod.NumTexCoords);
        }

        private void ExportCommonMeshData(FCustomArchiveWriter writer, CMeshSection[] sections, CMeshVertex[] verts,
            FRawStaticIndexBuffer indices, CVertexShare share, List<MaterialExporter>? materialExports)
        {
            var mainHdr = new VChunkHeader();
            var ptsHdr = new VChunkHeader();
            var wedgHdr = new VChunkHeader();
            var facesHdr = new VChunkHeader();
            var matrHdr = new VChunkHeader();

            mainHdr.TypeFlag = _PSK_VERSION;
            writer.SerializeChunkHeader(mainHdr, "ACTRHEAD");

            var numPoints = share.Points.Count;
            ptsHdr.DataCount = numPoints;
            ptsHdr.DataSize = 12;
            writer.SerializeChunkHeader(ptsHdr, "PNTS0000");
            for (var i = 0; i < numPoints; i++)
            {
                var point = share.Points[i];
                point.Y = -point.Y; // MIRROR_MESH
                point.Serialize(writer);
            }

            var numFaces = 0;
            var numVerts = verts.Length;
            var numSections = sections.Length;
            var wedgeMat = new int[numVerts];
            for (var i = 0; i < numSections; i++)
            {
                var faces = sections[i].NumFaces;
                numFaces += faces;
                for (var j = 0; j < faces * 3; j++)
                {
                    wedgeMat[indices[j + sections[i].FirstIndex]] = i;
                }
            }

            wedgHdr.DataCount = numVerts;
            wedgHdr.DataSize = 16;
            writer.SerializeChunkHeader(wedgHdr, "VTXW0000");
            for (var i = 0; i < numVerts; i++)
            {
                writer.Write(share.WedgeToVert[i]);
                writer.Write(verts[i].UV.U);
                writer.Write(verts[i].UV.V);
                writer.Write((byte) wedgeMat[i]);
                writer.Write((byte) 0);
                writer.Write((short) 0);
            }

            facesHdr.DataCount = numFaces;
            if (numVerts <= 65536)
            {
                facesHdr.DataSize = 12;
                writer.SerializeChunkHeader(facesHdr, "FACE0000");
                for (var i = 0; i < numSections; i++)
                {
                    for (var j = 0; j < sections[i].NumFaces; j++)
                    {
                        var wedgeIndex = new ushort[3];
                        for (var k = 0; k < wedgeIndex.Length; k++)
                        {
                            wedgeIndex[k] = (ushort)indices[sections[i].FirstIndex + j * 3 + k];
                        }

                        writer.Write(wedgeIndex[1]); // MIRROR_MESH
                        writer.Write(wedgeIndex[0]); // MIRROR_MESH
                        writer.Write(wedgeIndex[2]);
                        writer.Write((byte) i);
                        writer.Write((byte) 0);
                        writer.Write((uint) 1);
                    }
                }
            }
            else
            {
                facesHdr.DataSize = 18;
                writer.SerializeChunkHeader(facesHdr, "FACE3200");
                for (var i = 0; i < numSections; i++)
                {
                    for (var j = 0; j < sections[i].NumFaces; j++)
                    {
                        var wedgeIndex = new int[3];
                        for (var k = 0; k < wedgeIndex.Length; k++)
                        {
                            wedgeIndex[k] = indices[sections[i].FirstIndex + j * 3 + k];
                        }
                        
                        writer.Write(wedgeIndex[1]); // MIRROR_MESH
                        writer.Write(wedgeIndex[0]); // MIRROR_MESH
                        writer.Write(wedgeIndex[2]);
                        writer.Write((byte) i);
                        writer.Write((byte) 0);
                        writer.Write((uint) 1);
                    }
                }
            }

            matrHdr.DataCount = numSections;
            matrHdr.DataSize = 88;
            writer.SerializeChunkHeader(matrHdr, "MATT0000");
            for (var i = 0; i < numSections; i++)
            {
                string materialName;
                if (sections[i].Material?.Load<UMaterialInterface>() is { } tex)
                {
                    materialName = tex.Name;
                    materialExports?.Add(new MaterialExporter(tex, true));
                }
                else materialName = $"material_{i}";

                new VMaterial(materialName, i, 0u, 0, 0u, 0, 0).Serialize(writer);
            }
        }

        public void ExportVertexColors(FCustomArchiveWriter writer, FColor[]? colors, int numVerts)
        {
            if (colors == null) return;

            var colorHdr = new VChunkHeader {DataCount = numVerts, DataSize = 4};
            writer.SerializeChunkHeader(colorHdr, "VERTEXCOLOR");

            for (var i = 0; i < numVerts; i++)
            {
                colors[i].Serialize(writer);
            }
        }
        
        public void ExportExtraUV(FCustomArchiveWriter writer, FMeshUVFloat[][] extraUV, int numVerts, int numTexCoords)
        {
            var uvHdr = new VChunkHeader {DataCount = numVerts, DataSize = 8};
            for (var i = 1; i < numTexCoords; i++)
            {
                writer.SerializeChunkHeader(uvHdr, $"EXTRAUVS{i - 1}");
                for (var j = 0; j < numVerts; j++)
                {
                    extraUV[i - 1][j].Serialize(writer);
                }
            }
        }
        
        /// <param name="baseDirectory"></param>
        /// <param name="savedFileName"></param>
        /// <returns>true if *ALL* lods were successfully exported</returns>
        public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string savedFileName)
        {
            var b = false;
            savedFileName = MeshName.SubstringAfterLast('/');
            if (MeshLods.Count == 0) return b;

            var outText = "LOD ";
            for (var i = 0; i < MeshLods.Count; i++)
            {
                b |= MeshLods[i].TryWriteToDir(baseDirectory, out savedFileName);
                outText += $"{i} ";
            }

            savedFileName = outText + $"as '{savedFileName.SubstringAfterWithLast('.')}' for '{MeshName.SubstringAfterLast('/')}'";
            return b;
        }

        public override bool TryWriteToZip(out byte[] zipFile)
        {
            throw new NotImplementedException();
        }

        public override void AppendToZip()
        {
            throw new NotImplementedException();
        }
    }
}