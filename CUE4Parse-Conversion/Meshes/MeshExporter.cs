using System;
using System.Collections.Generic;
using System.IO;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Meshes.Common;
using CUE4Parse_Conversion.Meshes.PSK;
using Serilog;

namespace CUE4Parse_Conversion.Meshes
{
    public class MeshExporter : ExporterBase
    {
        private readonly string _meshName;
        private readonly Mesh[] _meshLods;
        
        public MeshExporter(UStaticMesh originalMesh, bool exportLods = false, bool exportMaterials = true)
        {
            _meshName = originalMesh.Name;
            if (!originalMesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Length <= 0)
            {
                Log.Logger.Warning($"Mesh {_meshName} has no LODs");
                _meshLods = new Mesh[0];
                return;
            }

            _meshLods = new Mesh[exportLods ? convertedMesh.LODs.Length : 1];
            for (var i = 0; i < _meshLods.Length; i++)
            {
                if (convertedMesh.LODs[i].Sections.Value.Length <= 0)
                {
                    Log.Logger.Warning($"LOD {i} in mesh {_meshName} has no section");
                    continue;
                }

                using var writer = new FCustomArchiveWriter();
                var materialExports = exportMaterials ? new List<MaterialExporter>() : null;
                ExportStaticMeshLods(convertedMesh.LODs[i], writer, materialExports);
                _meshLods[i] = new Mesh($"{_meshName}_LOD{i}.pskx", writer.GetBuffer(), materialExports ?? new List<MaterialExporter>());
            }
        }

        public MeshExporter(USkeletalMesh originalMesh)
        {
            if (!originalMesh.TryConvert()) return;
            throw new NotImplementedException();
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

        private void ExportCommonMeshData(FCustomArchiveWriter writer, CMeshSection[] sections, CMeshVertex[] verts,
            FRawStaticIndexBuffer indices, CVertexShare share, List<MaterialExporter>? materialExports)
        {
            var mainHdr = new VChunkHeader();
            var ptsHdr = new VChunkHeader();
            var wedgHdr = new VChunkHeader();
            var facesHdr = new VChunkHeader();
            var matrHdr = new VChunkHeader();

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
                if (sections[i].Material?.Value is { } tex)
                {
                    materialName = tex.Name;
                    materialExports?.Add(new MaterialExporter(tex));
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
        
        public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string savedFileName)
        {
            var b = false;
            savedFileName = _meshName;
            
            foreach (var meshLod in _meshLods)
            {
                b |= meshLod.TryWriteToDir(baseDirectory, out savedFileName);
            }
            
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