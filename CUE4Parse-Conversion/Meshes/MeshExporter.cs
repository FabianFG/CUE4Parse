using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Writers;
using CUE4Parse_Conversion.ActorX;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using Serilog;

namespace CUE4Parse_Conversion.Meshes
{
    public class MeshExporter : ExporterBase
    {
        public readonly List<Mesh> MeshLods;

        public MeshExporter(USkeleton originalSkeleton, ExporterOptions options) : base(originalSkeleton, options)
        {
            MeshLods = new List<Mesh>();

            if (!originalSkeleton.TryConvert(out var bones) || bones.Count == 0)
            {
                Log.Logger.Warning($"Skeleton '{ExportName}' has no bone");
                return;
            }

            using var Ar = new FArchiveWriter();

            var mainHdr = new VChunkHeader { TypeFlag = Constants.PSK_VERSION };
            Ar.SerializeChunkHeader(mainHdr, "ACTRHEAD");
            ExportSkeletalSockets(Ar, originalSkeleton.Sockets, bones);
            ExportSkeletonData(Ar, bones);

            MeshLods.Add(new Mesh($"{PackagePath}.psk", Ar.GetBuffer(), new List<MaterialExporter2>()));
        }

        public MeshExporter(UStaticMesh originalMesh, ExporterOptions options, bool exportMaterials = true) : base(originalMesh, options)
        {
            MeshLods = new List<Mesh>();

            if (!originalMesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count == 0)
            {
                Log.Logger.Warning($"Mesh '{ExportName}' has no LODs");
                return;
            }

            var i = -1;
            foreach (var lod in convertedMesh.LODs)
            {
                i++;
                if (lod.SkipLod)
                {
                    Log.Logger.Warning($"LOD {i} in mesh '{ExportName}' should be skipped");
                    continue;
                }

                using var Ar = new FArchiveWriter();
                var materialExports = exportMaterials ? new List<MaterialExporter2>() : null;
                string ext;
                switch (Options.MeshFormat)
                {
                    case EMeshFormat.ActorX:
                        ext = "pskx";
                        ExportStaticMeshLods(lod, Ar, materialExports, originalMesh.Sockets);
                        break;
                    case EMeshFormat.Gltf2:
                        ext = "glb";
                        new Gltf(ExportName, lod, materialExports, Options).Save(Options.MeshFormat, Ar);
                        break;
                    case EMeshFormat.OBJ:
                        ext = "obj";
                        new Gltf(ExportName, lod, materialExports, Options).Save(Options.MeshFormat, Ar);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Options.MeshFormat), Options.MeshFormat, null);
                }

                MeshLods.Add(new Mesh($"{PackagePath}_LOD{i}.{ext}", Ar.GetBuffer(), materialExports ?? new List<MaterialExporter2>()));
                if (Options.LodFormat == ELodFormat.FirstLod) break;
            }
        }

        public MeshExporter(USkeletalMesh originalMesh, ExporterOptions options, bool exportMaterials = true) : base(originalMesh, options)
        {
            MeshLods = new List<Mesh>();

            if (!originalMesh.TryConvert(out var convertedMesh) || convertedMesh.LODs.Count == 0)
            {
                Log.Logger.Warning($"Mesh '{ExportName}' has no LODs");
                return;
            }

            var totalSockets = new List<FPackageIndex>();
            if (Options.SocketFormat != ESocketFormat.None)
            {
                totalSockets.AddRange(originalMesh.Sockets);
                if (originalMesh.Skeleton.TryLoad<USkeleton>(out var originalSkeleton))
                {
                    totalSockets.AddRange(originalSkeleton.Sockets);
                }
            }

            var i = 0;
            for (var lodIndex = 0; lodIndex < convertedMesh.LODs.Count; lodIndex++)
            {
                var lod = convertedMesh.LODs[lodIndex];
                if (lod.SkipLod)
                {
                    Log.Logger.Warning($"LOD {i} in mesh '{ExportName}' should be skipped");
                    continue;
                }

                using var Ar = new FArchiveWriter();
                var materialExports = exportMaterials ? new List<MaterialExporter2>() : null;
                var ext = "";
                switch (Options.MeshFormat)
                {
                    case EMeshFormat.ActorX:
                        ext = convertedMesh.LODs[i].NumVerts > 65536 ? "pskx" : "psk";
                        ExportSkeletalMeshLod(lod, convertedMesh.RefSkeleton, Ar, materialExports,
                            Options.ExportMorphTargets ? originalMesh.MorphTargets : null,
                            totalSockets.ToArray(), lodIndex);
                        break;
                    case EMeshFormat.Gltf2:
                        ext = "glb";
                        new Gltf(ExportName, lod, convertedMesh.RefSkeleton, materialExports, Options,
                            Options.ExportMorphTargets ? originalMesh.MorphTargets : null, lodIndex).Save(Options.MeshFormat, Ar);
                        break;
                    case EMeshFormat.OBJ:
                        ext = "obj";
                        new Gltf(ExportName, lod, convertedMesh.RefSkeleton, materialExports, Options).Save(Options.MeshFormat, Ar);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Options.MeshFormat), Options.MeshFormat, null);
                }

                MeshLods.Add(new Mesh($"{PackagePath}_LOD{i}.{ext}", Ar.GetBuffer(), materialExports ?? new List<MaterialExporter2>()));
                if (Options.LodFormat == ELodFormat.FirstLod) break;
                i++;
            }
        }

        private void ExportStaticMeshLods(CStaticMeshLod lod, FArchiveWriter Ar, List<MaterialExporter2>? materialExports, FPackageIndex[] sockets)
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

            var bones = new List<CSkelMeshBone>();
            ExportStaticSockets(Ar, sockets, bones);
            ExportSkeletonData(Ar, bones);

            infHdr.DataCount = 0;
            infHdr.DataSize = 12;
            Ar.SerializeChunkHeader(infHdr, "RAWWEIGHTS");

            ExportVertexColors(Ar, lod.VertexColors, lod.NumVerts);
            ExportExtraUV(Ar, lod.ExtraUV.Value, lod.NumVerts, lod.NumTexCoords);
        }

        private void ExportSkeletalMeshLod(CSkelMeshLod lod, List<CSkelMeshBone> bones, FArchiveWriter Ar, List<MaterialExporter2>? materialExports, FPackageIndex[]? morphTargets, FPackageIndex[] sockets, int lodIndex)
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
            ExportSkeletalSockets(Ar, sockets, bones);
            ExportSkeletonData(Ar, bones);

            var numInfluences = 0;
            for (var i = 0; i < share.Points.Count; i++)
            {
                for (var j = 0; j < Constants.NUM_INFLUENCES_UE4; j++)
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

                for (var j = 0; j < Constants.NUM_INFLUENCES_UE4; j++)
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
            ExportMorphTargets(Ar, lod, share, morphTargets, lodIndex);
        }

        private void ExportCommonMeshData(FArchiveWriter Ar, CMeshSection[] sections, CMeshVertex[] verts,
            FRawStaticIndexBuffer indices, CVertexShare share, List<MaterialExporter2>? materialExports)
        {
            var mainHdr = new VChunkHeader();
            var ptsHdr = new VChunkHeader();
            var wedgHdr = new VChunkHeader();
            var facesHdr = new VChunkHeader();
            var matrHdr = new VChunkHeader();
            var normHdr = new VChunkHeader();

            mainHdr.TypeFlag = Constants.PSK_VERSION;
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
                    materialExports?.Add(new MaterialExporter2(tex, Options));
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
                if (i == 0) bone.BonePos.Orientation.W *= -1; // because the importer has invert enabled by default...
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

        public void ExportMorphTargets(FArchiveWriter Ar, CSkelMeshLod lod, CVertexShare share, FPackageIndex[]? morphTargets, int lodIndex)
        {
            if (morphTargets == null) return;

            var morphInfoHdr = new VChunkHeader { DataCount = morphTargets.Length, DataSize = 64 + sizeof(int) };
            Ar.SerializeChunkHeader(morphInfoHdr, "MRPHINFO");

            var morphDeltas = new List<VMorphData>();
            for (var i = 0; i < morphTargets.Length; i++)
            {
                var morphTarget = morphTargets[i].Load<UMorphTarget>();
                if (morphTarget?.MorphLODModels == null || morphTarget.MorphLODModels.Length <= lodIndex)
                    continue;

                var morphModel = morphTarget.MorphLODModels[lodIndex];
                var morphVertCount = 0;
                var localMorphDeltas = new List<VMorphData>();
                for (var j = 0; j < morphModel.Vertices.Length; j++)
                {
                    var delta = morphModel.Vertices[j];
                    var vertex = lod.Verts[delta.SourceIdx];

                    var index = FindVertex(vertex.Position, share.Points);
                    if (index == -1) continue;
                    if (localMorphDeltas.Any(x => x.PointIdx == index)) continue;

                    var morphData = new VMorphData(delta.PositionDelta, delta.TangentZDelta, index);
                    localMorphDeltas.Add(morphData);
                    morphVertCount++;
                }

                morphDeltas.AddRange(localMorphDeltas);

                var morphInfo = new VMorphInfo(morphTarget.Name, morphVertCount);
                morphInfo.Serialize(Ar);
            }

            var morphDataHdr = new VChunkHeader { DataCount = morphDeltas.Count, DataSize = Constants.VMorphData_SIZE };
            Ar.SerializeChunkHeader(morphDataHdr, "MRPHDATA");
            foreach (var delta in morphDeltas)
            {
                delta.Serialize(Ar);
            }
        }

        public void ExportSkeletalSockets(FArchiveWriter Ar, FPackageIndex[] sockets, List<CSkelMeshBone> bones)
        {
            if (sockets.Length == 0) return;
            switch (Options.SocketFormat)
            {
                case ESocketFormat.Socket:
                {
                    var socketInfoHdr = new VChunkHeader { DataCount = sockets.Length, DataSize = Constants.VSocket_SIZE };
                    Ar.SerializeChunkHeader(socketInfoHdr, "SKELSOCK");

                    for (var i = 0; i < sockets.Length; i++)
                    {
                        var socket = sockets[i].Load<USkeletalMeshSocket>();
                        if (socket is null) continue;

                        var pskSocket = new VSocket(socket.SocketName.Text, socket.BoneName.Text, socket.RelativeLocation, socket.RelativeRotation, socket.RelativeScale);
                        pskSocket.Serialize(Ar);
                    }

                    break;
                }
                case ESocketFormat.Bone:
                {
                    for (var i = 0; i < sockets.Length; i++)
                    {
                        var socket = sockets[i].Load<USkeletalMeshSocket>();
                        if (socket is null) continue;

                        var targetBoneIdx = -1;
                        for (var j = 0; j < bones.Count; j++)
                        {
                            if (bones[j].Name.Text.Equals(socket.BoneName.Text))
                            {
                                targetBoneIdx = j;
                                break;
                            }
                        }

                        if (targetBoneIdx == -1) continue;

                        var meshBone = new CSkelMeshBone
                        {
                            Name = socket.SocketName.Text,
                            ParentIndex = targetBoneIdx,
                            Position = socket.RelativeLocation,
                            Orientation = socket.RelativeRotation.Quaternion()
                        };

                        bones.Add(meshBone);
                    }

                    break;
                }
            }
        }
        public void ExportStaticSockets(FArchiveWriter Ar, FPackageIndex[] sockets, List<CSkelMeshBone> bones)
        {
            if (sockets.Length == 0) return;
            switch (Options.SocketFormat)
            {
                case ESocketFormat.Socket:
                {
                    var socketInfoHdr = new VChunkHeader { DataCount = sockets.Length, DataSize = Constants.VSocket_SIZE };
                    Ar.SerializeChunkHeader(socketInfoHdr, "SKELSOCK");

                    for (var i = 0; i < sockets.Length; i++)
                    {
                        var socket = sockets[i].Load<UStaticMeshSocket>();
                        if (socket is null) continue;

                        var pskSocket = new VSocket(socket.SocketName.Text, string.Empty, socket.RelativeLocation, socket.RelativeRotation, socket.RelativeScale);
                        pskSocket.Serialize(Ar);
                    }

                    break;
                }
                case ESocketFormat.Bone:
                {
                    for (var i = 0; i < sockets.Length; i++)
                    {
                        var socket = sockets[i].Load<UStaticMeshSocket>();
                        if (socket is null) continue;

                        var meshBone = new CSkelMeshBone
                        {
                            Name = socket.SocketName.Text,
                            ParentIndex = -1,
                            Position = socket.RelativeLocation,
                            Orientation = socket.RelativeRotation.Quaternion()
                        };

                        bones.Add(meshBone);
                    }

                    break;
                }
            }
        }

        private int FindVertex(FVector a, IReadOnlyList<FVector> vertices)
        {
            for (var i = 0; i < vertices.Count; i++)
            {
                if (vertices[i].Equals(a))
                    return i;
            }

            return -1;
        }

        /// <param name="baseDirectory"></param>
        /// <param name="label"></param>
        /// <param name="savedFilePath"></param>
        /// <returns>true if *ALL* lods were successfully exported</returns>
        public override bool TryWriteToDir(DirectoryInfo baseDirectory, out string label, out string savedFilePath)
        {
            var b = false;
            label = string.Empty;
            savedFilePath = PackagePath;
            if (MeshLods.Count == 0) return b;

            var outText = "LOD ";
            for (var i = 0; i < MeshLods.Count; i++)
            {
                b |= MeshLods[i].TryWriteToDir(baseDirectory, out label, out savedFilePath);
                outText += $"{i} ";
            }

            label = outText + $"as '{savedFilePath.SubstringAfterWithLast('.')}' for '{ExportName}'";
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
