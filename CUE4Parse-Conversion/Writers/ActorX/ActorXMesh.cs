using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.ActorX;
using CUE4Parse_Conversion.Writers.ActorX.Structs;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Writers.ActorX;

public class ActorXMesh
{
    private FArchiveWriter Ar;
    private readonly ExportOptions Options;

    public ActorXMesh(ExportOptions options)
    {
        Options = options;
        Ar = new FArchiveWriter();

        var mainHdr = new VChunkHeader { TypeFlag = Constants.PSK_VERSION };
        Ar.SerializeChunkHeader(mainHdr, "ACTRHEAD");
    }

    public ActorXMesh(SkeletonDto skeleton, ExportOptions options) : this(options)
    {
        ExportSkeletalSockets(skeleton);
        ExportSkeletonData(skeleton.Bones);
    }

    public ActorXMesh(MeshLodDto<MeshVertex> lod, ExportOptions options) : this(options)
    {
        ExportCommonMeshLod(lod);

        var bones = new List<MeshBoneDto>();
        if (lod.Owner.Sockets is { Length: > 0 } sockets)
        {
            ExportStaticSockets(sockets, bones);
        }

        // the legacy importer needs its REFSKELT chunk to initialize Bones and later do len(Bones)
        // https://github.com/Befzz/blender3d_import_psk_psa/blob/master/addons/io_import_scene_unreal_psa_psk_280.py#L711
        ExportSkeletonData(bones.ToArray());
    }

    public ActorXMesh(MeshLodDto<SkinnedMeshVertex> lod, ExportOptions options) : this(options)
    {
        if (lod.Owner is not SkeletalMeshDto mesh)
            throw new ArgumentException("LOD owner must be a SkeletalMeshDto for skeletal meshes.", nameof(lod));

        ExportCommonMeshLod(lod);

        var additionalBones = ExportSkeletalSockets(mesh);
        ExportSkeletonData([..mesh.Bones, ..additionalBones]);
    }

    public void Save(FArchiveWriter archive)
    {
        archive.Write(Ar.GetBuffer());
    }

    private void ExportCommonMeshLod<TVertex>(MeshLodDto<TVertex> lod) where TVertex : struct, IMeshVertex
    {
        var numInfluences = 0;
        foreach (var vert in lod.Vertices)
        {
            if (vert is SkinnedMeshVertex sv)
            {
                numInfluences += sv.Influences.Length;
            }
        }

        ExportCommonMeshData(lod);

        var infHdr = new VChunkHeader { DataCount = numInfluences, DataSize = 12 };
        Ar.SerializeChunkHeader(infHdr, "RAWWEIGHTS");
        if (infHdr.DataCount > 0)
        {
            for (var i = 0; i < lod.Vertices.Length; i++)
            {
                if (lod.Vertices[i] is not SkinnedMeshVertex v) continue;

                foreach (var influence in v.Influences)
                {
                    Ar.Write(influence.Weight);
                    Ar.Write(i);
                    Ar.Write((int) influence.Bone);
                }
            }
        }

        if (lod.VertexColors is { Length: > 0 })
        {
            ExportVertexColors(lod.VertexColors[0].Colors);
        }
        ExportExtraUV(lod.ExtraUvs);

        if (Options.ExportMorphTargets && lod.Owner is SkeletalMeshDto { MorphTargets: { Length: > 0 } morphTargets })
        {
            ExportMorphTargets(morphTargets, lod);
        }
    }

    private void ExportCommonMeshData<TVertex>(MeshLodDto<TVertex> lod) where TVertex : struct, IMeshVertex
    {
        var numPoints = lod.Vertices.Length;
        var ptsHdr = new VChunkHeader { DataCount = numPoints, DataSize = 12 };
        Ar.SerializeChunkHeader(ptsHdr, "PNTS0000");
        for (var i = 0; i < numPoints; i++)
        {
            var point = lod.Vertices[i].Position;
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

        var wedgHdr = new VChunkHeader {DataCount = numVerts, DataSize = 16};
        Ar.SerializeChunkHeader(wedgHdr, "VTXW0000");
        for (var i = 0; i < numVerts; i++)
        {
            Ar.Write(i);
            Ar.Write(lod.Vertices[i].Uv.U);
            Ar.Write(lod.Vertices[i].Uv.V);
            Ar.Write((byte) wedgeMat[i]);
            Ar.Write((byte) 0);
            Ar.Write((short) 0);
        }

        var facesHdr = new VChunkHeader { DataCount = numFaces };
        if (numVerts <= 65536)
        {
            facesHdr.DataSize = 12;
            Ar.SerializeChunkHeader(facesHdr, "FACE0000");
            Span<ushort> wedgeIndex = stackalloc ushort[3];
            for (var i = 0; i < numSections; i++)
            {
                var section = (ushort) (i & 0xFF);
                var index = lod.Sections[i].FirstIndex;
                for (var j = 0; j < lod.Sections[i].NumFaces; j++, index += 3)
                {
                    for (var k = 0; k < wedgeIndex.Length; k++)
                    {
                        wedgeIndex[k] = (ushort) lod.Indices[index + k];
                    }

                    Ar.Write(wedgeIndex[1]); // MIRROR_MESH
                    Ar.Write(wedgeIndex[0]); // MIRROR_MESH
                    Ar.Write(wedgeIndex[2]);
                    Ar.Write(section);
                    Ar.Write(1);
                }
            }
        }
        else
        {
            facesHdr.DataSize = 18;
            Ar.SerializeChunkHeader(facesHdr, "FACE3200");
            Span<uint> wedgeIndex = stackalloc uint[3];
            for (var i = 0; i < numSections; i++)
            {
                var section = (ushort) (i & 0xFF);
                var index = lod.Sections[i].FirstIndex;
                for (var j = 0; j < lod.Sections[i].NumFaces; j++, index += 3)
                {
                    for (var k = 0; k < wedgeIndex.Length; k++)
                    {
                        wedgeIndex[k] = lod.Indices[index + k];
                    }

                    Ar.Write(wedgeIndex[1]); // MIRROR_MESH
                    Ar.Write(wedgeIndex[0]); // MIRROR_MESH
                    Ar.Write(wedgeIndex[2]);
                    Ar.Write(section);
                    Ar.Write(1);
                }
            }
        }

        var matrHdr = new VChunkHeader { DataCount = numSections, DataSize = 88};
        Ar.SerializeChunkHeader(matrHdr, "MATT0000");
        for (var i = 0; i < numSections; i++)
        {
            var materialName = lod.Owner.GetMaterial(lod.Sections[i])?.SlotName ?? $"MaterialSlot_{i}";
            new VMaterial(materialName, i, 0u, 0, 0u, 0, 0).Serialize(Ar);
        }

        var numNormals = lod.Vertices.Length;
        var normHdr = new VChunkHeader {DataCount = numNormals,  DataSize = 12};
        Ar.SerializeChunkHeader(normHdr, "VTXNORMS");
        for (var i = 0; i < numNormals; i++)
        {
            var normal = (FVector) lod.Vertices[i].Normal;

            // Normalize
            normal /= MathF.Sqrt(normal | normal);

            normal.Y = -normal.Y; // MIRROR_MESH
            normal.Serialize(Ar);
        }
    }

    private void ExportSkeletonData(MeshBoneDto[] bones)
    {
        var numBones = bones.Length;
        var boneHdr = new VChunkHeader {DataCount = numBones, DataSize = 120};
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

    public void ExportVertexColors(FColor[] colors)
    {
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

    private void ExportMorphTargets<TVertex>(FPackageIndex[] morphTargets, MeshLodDto<TVertex> lod) where TVertex : struct, IMeshVertex
    {
        var morphInfoHdr = new VChunkHeader { DataCount = morphTargets.Length, DataSize = 64 + sizeof(int) };
        Ar.SerializeChunkHeader(morphInfoHdr, "MRPHINFO");

        var morphDeltas = new List<VMorphData>();
        for (var i = 0; i < morphTargets.Length; i++)
        {
            var morphTarget = morphTargets[i].Load<UMorphTarget>();
            if (morphTarget?.MorphLODModels == null || morphTarget.MorphLODModels.Length <= lod.SourceLodIndex ||
                morphTarget.MorphLODModels[lod.SourceLodIndex].Vertices.Length == 0)
            {
                var emptyMorphInfo = new VMorphInfo(morphTarget?.Name ?? $"UnknownMorph_{i}", 0);
                emptyMorphInfo.Serialize(Ar);
                continue;
            }

            var morphModel = morphTarget.MorphLODModels[lod.SourceLodIndex];
            var localMorphDeltas = new List<VMorphData>(morphModel.Vertices.Length);
            for (var j = 0; j < morphModel.Vertices.Length; j++)
            {
                var delta = morphModel.Vertices[j];
                if (delta.SourceIdx >= lod.Vertices.Length) continue;

                var morphData = new VMorphData(delta.PositionDelta, delta.TangentZDelta, (int) delta.SourceIdx);
                localMorphDeltas.Add(morphData);
            }

            morphDeltas.AddRange(localMorphDeltas);

            var morphInfo = new VMorphInfo(morphTarget.Name, localMorphDeltas.Count);
            morphInfo.Serialize(Ar);
        }

        var morphDataHdr = new VChunkHeader { DataCount = morphDeltas.Count, DataSize = Constants.VMorphData_SIZE };
        Ar.SerializeChunkHeader(morphDataHdr, "MRPHDATA");
        foreach (var delta in morphDeltas)
        {
            delta.Serialize(Ar);
        }
    }

    public MeshBoneDto[] ExportSkeletalSockets(SkeletonDto skeleton)
    {
        if (skeleton.Sockets is not { Length: > 0 } sockets) return [];

        switch (Options.SocketFormat)
        {
            case ESocketFormat.Socket:
            {
                var validSockets = new List<VSocket>(sockets.Length);
                for (var i = 0; i < sockets.Length; i++)
                {
                    var socket = sockets[i].Load<USkeletalMeshSocket>();
                    if (socket is null) continue;

                    var pskSocket = new VSocket(socket.SocketName.Text, socket.BoneName.Text, socket.RelativeLocation, socket.RelativeRotation, socket.RelativeScale);
                    validSockets.Add(pskSocket);
                }

                if (validSockets.Count > 0)
                {
                    var socketInfoHdr = new VChunkHeader { DataCount = validSockets.Count, DataSize = Constants.VSocket_SIZE };
                    Ar.SerializeChunkHeader(socketInfoHdr, "SKELSOCK");
                    foreach (var socket in validSockets)
                    {
                        socket.Serialize(Ar);
                    }
                }

                return [];
            }
            case ESocketFormat.Bone:
            {
                var additionalBones = new List<MeshBoneDto>();
                for (var i = 0; i < sockets.Length; i++)
                {
                    var socket = sockets[i].Load<USkeletalMeshSocket>();
                    if (socket is null) continue;

                    var targetBoneIdx = -1;
                    for (var j = 0; j < skeleton.Bones.Length; j++)
                    {
                        if (skeleton.Bones[j].Name.Equals(socket.BoneName.Text, StringComparison.OrdinalIgnoreCase))
                        {
                            targetBoneIdx = j;
                            break;
                        }
                    }

                    if (targetBoneIdx == -1) continue;
                    additionalBones.Add(new MeshBoneDto(socket, targetBoneIdx));
                }

                return additionalBones.ToArray();
            }
            default: return [];
        }
    }
    public void ExportStaticSockets(FPackageIndex[] sockets, List<MeshBoneDto> bones)
    {
        if (sockets.Length == 0) return;
        switch (Options.SocketFormat)
        {
            case ESocketFormat.Socket:
            {
                var validSockets = new List<VSocket>(sockets.Length);
                for (var i = 0; i < sockets.Length; i++)
                {
                    var socket = sockets[i].Load<UStaticMeshSocket>();
                    if (socket is null) continue;

                    var pskSocket = new VSocket(socket.SocketName.Text, string.Empty, socket.RelativeLocation, socket.RelativeRotation, socket.RelativeScale);
                    validSockets.Add(pskSocket);
                }

                if (validSockets.Count > 0)
                {
                    var socketInfoHdr = new VChunkHeader { DataCount = validSockets.Count, DataSize = Constants.VSocket_SIZE };
                    Ar.SerializeChunkHeader(socketInfoHdr, "SKELSOCK");
                    foreach (var socket in validSockets)
                    {
                        socket.Serialize(Ar);
                    }
                }

                break;
            }
            case ESocketFormat.Bone:
            {
                for (var i = 0; i < sockets.Length; i++)
                {
                    var socket = sockets[i].Load<UStaticMeshSocket>();
                    if (socket is null) continue;

                    bones.Add(new MeshBoneDto(socket));
                }

                break;
            }
        }
    }
}
