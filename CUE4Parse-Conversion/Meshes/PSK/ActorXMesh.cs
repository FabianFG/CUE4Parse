using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.ActorX;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.PSK;

public class ActorXMesh
{
    private FArchiveWriter Ar;
    private readonly ExporterOptions Options;

    public ActorXMesh(ExporterOptions options)
    {
        Options = options;
        Ar = new FArchiveWriter();

        var mainHdr = new VChunkHeader { TypeFlag = Constants.PSK_VERSION };
        Ar.SerializeChunkHeader(mainHdr, "ACTRHEAD");
    }

    public ActorXMesh(Skeleton skeleton, ExporterOptions options) : this(options)
    {
        ExportSkeletalSockets(skeleton);
        ExportSkeletonData(skeleton.RefSkeleton);
    }

    public ActorXMesh(StaticMesh mesh, ExporterOptions options, int lodIndex = -1) : this(options)
    {
        ExportCommonMeshLod(mesh, lodIndex);

        if (mesh.Sockets is { Length: > 0 } sockets)
        {
            var bones = new List<MeshBone>();
            ExportStaticSockets(sockets, bones);
            ExportSkeletonData(bones);
        }
    }

    public ActorXMesh(SkeletalMesh mesh, ExporterOptions options, int lodIndex = -1) : this(options)
    {
        ExportCommonMeshLod(mesh, lodIndex);

        ExportSkeletalSockets(mesh);
        ExportSkeletonData(mesh.RefSkeleton);
    }

    public void Save(FArchiveWriter archive)
    {
        archive.Write(Ar.GetBuffer());
    }

    private void ExportCommonMeshLod<TVertex>(Mesh<TVertex> mesh, int lodIndex = -1) where TVertex : struct, IMeshVertex
    {
        if (lodIndex < 0)
        {
            for (var i = 0; i < mesh.LODs.Count; )
            {
                lodIndex = i;
                break;
            }
        }

        var lod = mesh.LODs[lodIndex];

        var numInfluences = 0;
        var share = new CVertexShare();
        share.Prepare(lod.Vertices);
        foreach (var vert in lod.Vertices)
        {
            var weightsHash = 0u;
            if (vert is SkinnedMeshVertex skinnedVert)
            {
                weightsHash = (uint)StructuralComparisons.StructuralEqualityComparer.GetHashCode(skinnedVert.Influences);
                numInfluences += skinnedVert.Influences.Count;
            }

            share.AddVertex(vert.Position, vert.Normal, weightsHash);
        }

        ExportCommonMeshData(lod, share);

        var infHdr = new VChunkHeader { DataCount = numInfluences, DataSize = 12 };
        Ar.SerializeChunkHeader(infHdr, "RAWWEIGHTS");
        if (infHdr.DataCount > 0)
        {
            for (var i = 0; i < share.Points.Count; i++)
            {
                if (lod.Vertices[share.VertToWedge.Value[i]] is not SkinnedMeshVertex v) continue;

                foreach (var influence in v.Influences)
                {
                    Ar.Write(influence.Weight);
                    Ar.Write(i);
                    Ar.Write((int) influence.Bone);
                }
            }
        }

        ExportVertexColors(lod.VertexColors);
        ExportExtraUV(lod.ExtraUvs);

        if (mesh is SkeletalMesh sk)
        {
            ExportMorphTargets(sk.MorphTargets, lod, share, lodIndex);
        }
    }

    private void ExportCommonMeshData<TVertex>(MeshLod<TVertex> lod, CVertexShare share) where TVertex : struct, IMeshVertex
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
        var numVerts = lod.Vertices.Length;
        var numSections = lod.Sections.Length;
        var wedgeMat = new int[numVerts];
        for (var i = 0; i < numSections; i++)
        {
            var faces = lod.Sections[i].NumFaces;
            numFaces += faces;
            for (var j = 0; j < faces * 3; j++)
            {
                wedgeMat[lod.Indices[j + lod.Sections[i].FirstIndex]] = i;
            }
        }

        wedgHdr.DataCount = numVerts;
        wedgHdr.DataSize = 16;
        Ar.SerializeChunkHeader(wedgHdr, "VTXW0000");
        for (var i = 0; i < numVerts; i++)
        {
            Ar.Write(share.WedgeToVert[i]);
            Ar.Write(lod.Vertices[i].Uv.U);
            Ar.Write(lod.Vertices[i].Uv.V);
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
                for (var j = 0; j < lod.Sections[i].NumFaces; j++)
                {
                    var wedgeIndex = new ushort[3];
                    for (var k = 0; k < wedgeIndex.Length; k++)
                    {
                        wedgeIndex[k] = (ushort) lod.Indices[lod.Sections[i].FirstIndex + j * 3 + k];
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
                for (var j = 0; j < lod.Sections[i].NumFaces; j++)
                {
                    var wedgeIndex = new uint[3];
                    for (var k = 0; k < wedgeIndex.Length; k++)
                    {
                        wedgeIndex[k] = lod.Indices[lod.Sections[i].FirstIndex + j * 3 + k];
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
            var materialName = lod.Owner.GetMaterial(lod.Sections[i])?.SlotName ?? $"MaterialSlot_{i}";
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

    private void ExportSkeletonData(List<MeshBone> bones)
    {
        if (bones.Count == 0) return;

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
                Name = bones[i].Name,
                NumChildren = numChildren,
                ParentIndex = bones[i].ParentIndex,
                BonePos = new VJointPosPsk
                {
                    Position = bones[i].Transform.Translation,
                    Orientation = bones[i].Transform.Rotation
                }
            };

            // MIRROR_MESH
            bone.BonePos.Orientation.Y *= -1;
            if (i == 0) bone.BonePos.Orientation.W *= -1; // because the importer has invert enabled by default...
            bone.BonePos.Position.Y *= -1;

            bone.Serialize(Ar);
        }
    }

    public void ExportVertexColors(IReadOnlyDictionary<string, FColor[]>? vertexColors)
    {
        if (vertexColors == null || !vertexColors.TryGetValue("COL0", out var colors)) return;

        var colorHdr = new VChunkHeader { DataCount = colors.Length, DataSize = 4 };
        Ar.SerializeChunkHeader(colorHdr, "VERTEXCOLOR");
        for (var i = 0; i < colorHdr.DataCount; i++)
        {
            colors[i].Serialize(Ar);
        }
    }

    public void ExportExtraUV(FMeshUVFloat[][] extraUvs)
    {
        for (var i = 0; i < extraUvs.Length; i++)
        {
            var uvHdr = new VChunkHeader { DataCount = extraUvs[i].Length, DataSize = 8 };
            Ar.SerializeChunkHeader(uvHdr, $"EXTRAUVS{i}");
            for (var j = 0; j < uvHdr.DataCount; j++)
            {
                extraUvs[i][j].Serialize(Ar);
            }
        }
    }

    public void ExportMorphTargets<TVertex>(FPackageIndex[]? morphTargets, MeshLod<TVertex> lod, CVertexShare share, int lodIndex) where TVertex : struct, IMeshVertex
    {
        if (!Options.ExportMorphTargets || morphTargets == null) return;

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
                if (delta.SourceIdx >= lod.Vertices.Length) continue;

                var vertex = lod.Vertices[delta.SourceIdx];

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

    public void ExportSkeletalSockets(Skeleton skeleton)
    {
        if (skeleton.Sockets is not { Length: > 0 } sockets) return;

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
                    for (var j = 0; j < skeleton.RefSkeleton.Count; j++)
                    {
                        if (skeleton.RefSkeleton[j].Name.Equals(socket.BoneName.Text, StringComparison.OrdinalIgnoreCase))
                        {
                            targetBoneIdx = j;
                            break;
                        }
                    }

                    if (targetBoneIdx == -1) continue;
                    skeleton.RefSkeleton.Add(new MeshBone(socket, targetBoneIdx));
                }

                break;
            }
        }
    }
    public void ExportStaticSockets(FPackageIndex[] sockets, List<MeshBone> bones)
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

                    bones.Add(new MeshBone(socket));
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

}
