using System;
using System.Collections.Generic;
using System.IO;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Writers;
using CUE4Parse.Utils;
using CUE4Parse_Conversion.ActorX;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Meshes.PSK;
using Serilog;

namespace CUE4Parse_Conversion.Meshes
{
    public class MeshExporter : ExporterBase
    {
        private const int _PSK_VERSION = 20210917;

        public readonly string MeshName;
        public readonly List<Mesh> MeshLods;

        public MeshExporter(USkeleton originalSkeleton)
        {
            MeshLods = new List<Mesh>();
            MeshName = originalSkeleton.Owner?.Name ?? originalSkeleton.Name;

            if (!originalSkeleton.TryConvert(out var bones) || bones.Count == 0)
            {
                Log.Logger.Warning($"Skeleton '{MeshName}' has no bone");
                return;
            }

            using var Ar = new FArchiveWriter();

            var mainHdr = new VChunkHeader { TypeFlag = _PSK_VERSION };
            Ar.SerializeChunkHeader(mainHdr, "ACTRHEAD");
            ExportSkeletonData(Ar, bones);

            MeshLods.Add(new Mesh($"{MeshName}.psk", Ar.GetBuffer(), new List<MaterialExporter>()));
        }

        public MeshExporter(UStaticMesh originalMesh, ELodFormat lodFormat = ELodFormat.FirstLod, bool exportMaterials = true, EMeshFormat meshFormat = EMeshFormat.ActorX)
        {
            MeshLods = new List<Mesh>();
            MeshName = originalMesh.Owner?.Name ?? originalMesh.Name;

            if (!originalMesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count == 0)
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

                using var Ar = new FArchiveWriter();
                var materialExports = exportMaterials ? new List<MaterialExporter>() : null;
                var ext = "";
                switch (meshFormat)
                {
                    case EMeshFormat.ActorX:
                        ext = "pskx";
                        ExportStaticMeshLods(lod, Ar, materialExports);
                        break;
                    case EMeshFormat.Gltf2:
                        ext = "glb";
                        new Gltf(MeshName.SubstringAfterLast("/"), lod, Ar, materialExports, meshFormat);
                        break;
                    case EMeshFormat.OBJ:
                        ext = "obj";
                        new Gltf(MeshName.SubstringAfterLast("/"), lod, Ar, materialExports, meshFormat);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(meshFormat), meshFormat, null);
                }

                MeshLods.Add(new Mesh($"{MeshName}_LOD{i}.{ext}", Ar.GetBuffer(), materialExports ?? new List<MaterialExporter>()));
                if (lodFormat == ELodFormat.FirstLod) break;
                i++;
            }
        }

        public MeshExporter(USkeletalMesh originalMesh, ELodFormat lodFormat = ELodFormat.FirstLod, bool exportMaterials = true, EMeshFormat meshFormat = EMeshFormat.ActorX)
        {
            MeshLods = new List<Mesh>();
            MeshName = originalMesh.Owner?.Name ?? originalMesh.Name;

            if (!originalMesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count == 0)
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

                using var Ar = new FArchiveWriter();
                var materialExports = exportMaterials ? new List<MaterialExporter>() : null;
                var ext = "";
                switch (meshFormat)
                {
                    case EMeshFormat.ActorX:
                        ext = convertedMesh.LODs[i].NumVerts > 65536 ? "pskx" : "psk";
                        ExportSkeletalMeshLod(lod, convertedMesh.RefSkeleton, Ar, materialExports);
                        break;
                    case EMeshFormat.Gltf2:
                        ext = "glb";
                        new Gltf(MeshName.SubstringAfterLast("/"), lod, convertedMesh.RefSkeleton, Ar, materialExports, meshFormat);
                        break;
                    case EMeshFormat.OBJ:
                        ext = "obj";
                        new Gltf(MeshName.SubstringAfterLast("/"), lod, convertedMesh.RefSkeleton, Ar, materialExports, meshFormat);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(meshFormat), meshFormat, null);
                }

                MeshLods.Add(new Mesh($"{MeshName}_LOD{i}.{ext}", Ar.GetBuffer(), materialExports ?? new List<MaterialExporter>()));
                if (lodFormat == ELodFormat.FirstLod) break;
                i++;
            }
        }


        private void ExportStaticMeshLods(CStaticMeshLod lod, FArchiveWriter Ar, List<MaterialExporter>? materialExports)
        {
            var share = new CVertexShare();
            var boneHdr = new VChunkHeader();
            var infHdr = new VChunkHeader();

            share.Prepare(lod.Verts);
            foreach (var vert in lod.Verts)
            {
                share.AddVertex(vert.Position, vert.Normal);
            }

            ExportCommonMeshData(Ar, lod.Sections.Value, lod.Verts, lod.Indices.Value, share, materialExports);

            boneHdr.DataCount = 0;
            boneHdr.DataSize = 120;
            Ar.SerializeChunkHeader(boneHdr, "REFSKELT");

            infHdr.DataCount = 0;
            infHdr.DataSize = 12;
            Ar.SerializeChunkHeader(infHdr, "RAWWEIGHTS");

            ExportVertexColors(Ar, lod.VertexColors, lod.NumVerts);
            ExportExtraUV(Ar, lod.ExtraUV.Value, lod.NumVerts, lod.NumTexCoords);
        }

        private void ExportSkeletalMeshLod(CSkelMeshLod lod, List<CSkelMeshBone> bones, FArchiveWriter Ar, List<MaterialExporter>? materialExports)
        {
            var share = new CVertexShare();
            var infHdr = new VChunkHeader();

            share.Prepare(lod.Verts);
            foreach (var vert in lod.Verts)
            {
                var weightsHash = vert.PackedWeights;
                for (var i = 0; i < vert.Bone.Length; i++)
                {
                    weightsHash ^= (uint) vert.Bone[i] << i;
                }

                share.AddVertex(vert.Position, vert.Normal, weightsHash);
            }

            ExportCommonMeshData(Ar, lod.Sections.Value, lod.Verts, lod.Indices.Value, share, materialExports);
            ExportSkeletonData(Ar, bones);

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
            Ar.SerializeChunkHeader(infHdr, "RAWWEIGHTS");
            for (var i = 0; i < share.Points.Count; i++)
            {
                var v = lod.Verts[share.VertToWedge.Value[i]];
                var unpackedWeights = v.UnpackWeights();

                for (var j = 0; j < 4; j++)
                {
                    if (v.Bone[j] < 0)
                        break;

                    Ar.Write(unpackedWeights[j]);
                    Ar.Write(i);
                    Ar.Write((int) v.Bone[j]);
                }
            }

            ExportVertexColors(Ar, lod.VertexColors, lod.NumVerts);
            ExportExtraUV(Ar, lod.ExtraUV.Value, lod.NumVerts, lod.NumTexCoords);
        }

        private void ExportCommonMeshData(FArchiveWriter Ar, CMeshSection[] sections, CMeshVertex[] verts,
            FRawStaticIndexBuffer indices, CVertexShare share, List<MaterialExporter>? materialExports)
        {
            var mainHdr = new VChunkHeader();
            var ptsHdr = new VChunkHeader();
            var wedgHdr = new VChunkHeader();
            var facesHdr = new VChunkHeader();
            var matrHdr = new VChunkHeader();
            var normHdr = new VChunkHeader();

            mainHdr.TypeFlag = _PSK_VERSION;
            Ar.SerializeChunkHeader(mainHdr, "ACTRHEAD");

            var numPoints = share.Points.Count;
            ptsHdr.DataCount = numPoints;
            ptsHdr.DataSize = 12;
            Ar.SerializeChunkHeader(ptsHdr, "PNTS0000");
            for (var i = 0; i < numPoints; i++)
            {
                var point = share.Points[i];
                point.Y = -point.Y; // MIRROR_MESH
                point.Serialize(Ar);
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
            Ar.SerializeChunkHeader(wedgHdr, "VTXW0000");
            for (var i = 0; i < numVerts; i++)
            {
                Ar.Write(share.WedgeToVert[i]);
                Ar.Write(verts[i].UV.U);
                Ar.Write(verts[i].UV.V);
                Ar.Write((byte) wedgeMat[i]);
                Ar.Write((byte) 0);
                Ar.Write((short) 0);
            }

            facesHdr.DataCount = numFaces;
            if (numVerts <= 65536)
            {
                facesHdr.DataSize = 12;
                Ar.SerializeChunkHeader(facesHdr, "FACE0000");
                for (var i = 0; i < numSections; i++)
                {
                    for (var j = 0; j < sections[i].NumFaces; j++)
                    {
                        var wedgeIndex = new ushort[3];
                        for (var k = 0; k < wedgeIndex.Length; k++)
                        {
                            wedgeIndex[k] = (ushort) indices[sections[i].FirstIndex + j * 3 + k];
                        }

                        Ar.Write(wedgeIndex[1]); // MIRROR_MESH
                        Ar.Write(wedgeIndex[0]); // MIRROR_MESH
                        Ar.Write(wedgeIndex[2]);
                        Ar.Write((byte) i);
                        Ar.Write((byte) 0);
                        Ar.Write((uint) 1);
                    }
                }
            }
            else
            {
                facesHdr.DataSize = 18;
                Ar.SerializeChunkHeader(facesHdr, "FACE3200");
                for (var i = 0; i < numSections; i++)
                {
                    for (var j = 0; j < sections[i].NumFaces; j++)
                    {
                        var wedgeIndex = new int[3];
                        for (var k = 0; k < wedgeIndex.Length; k++)
                        {
                            wedgeIndex[k] = indices[sections[i].FirstIndex + j * 3 + k];
                        }

                        Ar.Write(wedgeIndex[1]); // MIRROR_MESH
                        Ar.Write(wedgeIndex[0]); // MIRROR_MESH
                        Ar.Write(wedgeIndex[2]);
                        Ar.Write((byte) i);
                        Ar.Write((byte) 0);
                        Ar.Write((uint) 1);
                    }
                }
            }

            matrHdr.DataCount = numSections;
            matrHdr.DataSize = 88;
            Ar.SerializeChunkHeader(matrHdr, "MATT0000");
            for (var i = 0; i < numSections; i++)
            {
                string materialName;
                if (sections[i].Material?.Load<UMaterialInterface>() is { } tex)
                {
                    materialName = tex.Name;
                    materialExports?.Add(new MaterialExporter(tex, true));
                }
                else materialName = $"material_{i}";

                new VMaterial(materialName, i, 0u, 0, 0u, 0, 0).Serialize(Ar);
            }

            var numNormals = share.Normals.Count;
            normHdr.DataCount = numNormals;
            normHdr.DataSize = 12;
            Ar.SerializeChunkHeader(normHdr, "VTXNORMS");
            for (var i = 0; i < numNormals; i++)
            {
                var normal = (FVector)share.Normals[i];

                // Normalize
                normal /= MathF.Sqrt(normal | normal);

                normal.Y = -normal.Y; // MIRROR_MESH
                normal.Serialize(Ar);
            }
        }

        private void ExportSkeletonData(FArchiveWriter Ar, List<CSkelMeshBone> bones)
        {
            var boneHdr = new VChunkHeader();

            var numBones = bones.Count;
            boneHdr.DataCount = numBones;
            boneHdr.DataSize = 120;
            Ar.SerializeChunkHeader(boneHdr, "REFSKELT");
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

                bone.Serialize(Ar);
            }
        }

        public void ExportVertexColors(FArchiveWriter Ar, FColor[]? colors, int numVerts)
        {
            if (colors == null) return;

            var colorHdr = new VChunkHeader { DataCount = numVerts, DataSize = 4 };
            Ar.SerializeChunkHeader(colorHdr, "VERTEXCOLOR");

            for (var i = 0; i < numVerts; i++)
            {
                colors[i].Serialize(Ar);
            }
        }

        public void ExportExtraUV(FArchiveWriter Ar, FMeshUVFloat[][] extraUV, int numVerts, int numTexCoords)
        {
            var uvHdr = new VChunkHeader { DataCount = numVerts, DataSize = 8 };
            for (var i = 1; i < numTexCoords; i++)
            {
                Ar.SerializeChunkHeader(uvHdr, $"EXTRAUVS{i - 1}");
                for (var j = 0; j < numVerts; j++)
                {
                    extraUV[i - 1][j].Serialize(Ar);
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
