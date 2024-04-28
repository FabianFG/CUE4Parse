using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.ActorX;
using CUE4Parse_Conversion.Materials;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
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

    public ActorXMesh(List<CSkelMeshBone> bones, FPackageIndex[] sockets, ExporterOptions options) : this(options)
    {
        ExportSkeletalSockets(sockets, bones);
        ExportSkeletonData(bones);
    }
    
    public ActorXMesh(CStaticMeshLod lod, List<MaterialExporter2>? materialExports, FPackageIndex[] sockets, ExporterOptions options) : this(options)
    {
        ExportStaticMeshLods(lod, materialExports, sockets);
    }
    
    public ActorXMesh(CSkelMeshLod lod, List<CSkelMeshBone> refSkeleton, List<MaterialExporter2>? materialExports, FPackageIndex[]? morphTargets,  FPackageIndex[] sockets, int lodIndex, ExporterOptions options) : this(options)
    {
        ExportSkeletalMeshLod(lod, refSkeleton, materialExports, morphTargets, sockets, lodIndex);
    }
    
    public void Save(FArchiveWriter archive)
    {
        archive.Write(Ar.GetBuffer());
    }
    
    private void ExportStaticMeshLods(CStaticMeshLod lod, List<MaterialExporter2>? materialExports, FPackageIndex[] sockets)
    {
        var share = new CVertexShare();
        var infHdr = new VChunkHeader();

        share.Prepare(lod.Verts);
        foreach (var vert in lod.Verts)
        {
            share.AddVertex(vert.Position, vert.Normal);
        }

        ExportCommonMeshData(lod.Sections.Value, lod.Verts, lod.Indices.Value, share, materialExports);

        var bones = new List<CSkelMeshBone>();
        ExportStaticSockets(sockets, bones);
        ExportSkeletonData(bones);

        infHdr.DataCount = 0;
        infHdr.DataSize = 12;
        Ar.SerializeChunkHeader(infHdr, "RAWWEIGHTS");

        ExportVertexColors(lod.VertexColors, lod.NumVerts);
        ExportExtraUV(lod.ExtraUV.Value, lod.NumVerts, lod.NumTexCoords);
    }

    private void ExportSkeletalMeshLod(CSkelMeshLod lod, List<CSkelMeshBone> bones, List<MaterialExporter2>? materialExports, FPackageIndex[]? morphTargets, FPackageIndex[] sockets, int lodIndex)
    {
        var share = new CVertexShare();
        var infHdr = new VChunkHeader();

        share.Prepare(lod.Verts);
        foreach (var vert in lod.Verts)
        {
            var weightsHash = (uint)StructuralComparisons.StructuralEqualityComparer.GetHashCode(vert.Influences);
            share.AddVertex(vert.Position, vert.Normal, weightsHash);
        }

        ExportCommonMeshData(lod.Sections.Value, lod.Verts, lod.Indices.Value, share, materialExports);
        ExportSkeletalSockets(sockets, bones);
        ExportSkeletonData(bones);

        var numInfluences = 0;
        for (var i = 0; i < share.Points.Count; i++)
        {
            numInfluences += lod.Verts[share.VertToWedge.Value[i]].Influences.Count;
        }
        infHdr.DataCount = numInfluences;
        infHdr.DataSize = 12;
        Ar.SerializeChunkHeader(infHdr, "RAWWEIGHTS");
        for (var i = 0; i < share.Points.Count; i++)
        {
            var v = lod.Verts[share.VertToWedge.Value[i]];

            foreach (var influence in v.Influences)
            {
                Ar.Write(influence.Weight);
                Ar.Write(i);
                Ar.Write((int) influence.Bone);
            }
        }

        ExportVertexColors(lod.VertexColors, lod.NumVerts);
        ExportExtraUV(lod.ExtraUV.Value, lod.NumVerts, lod.NumTexCoords);
        ExportMorphTargets(lod, share, morphTargets, lodIndex);
    }

    private void ExportCommonMeshData(CMeshSection[] sections, CMeshVertex[] verts,
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

    private void ExportSkeletonData(List<CSkelMeshBone> bones)
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

    public void ExportVertexColors(FColor[]? colors, int numVerts)
    {
        if (colors == null) return;

        var colorHdr = new VChunkHeader { DataCount = numVerts, DataSize = 4 };
        Ar.SerializeChunkHeader(colorHdr, "VERTEXCOLOR");

        for (var i = 0; i < numVerts; i++)
        {
            colors[i].Serialize(Ar);
        }
    }

    public void ExportExtraUV(FMeshUVFloat[][] extraUV, int numVerts, int numTexCoords)
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

    public void ExportMorphTargets(CSkelMeshLod lod, CVertexShare share, FPackageIndex[]? morphTargets, int lodIndex)
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
                if (delta.SourceIdx >= lod.Verts.Length) continue;

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

    public void ExportSkeletalSockets(FPackageIndex[] sockets, List<CSkelMeshBone> bones)
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
    public void ExportStaticSockets(FPackageIndex[] sockets, List<CSkelMeshBone> bones)
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
    
}