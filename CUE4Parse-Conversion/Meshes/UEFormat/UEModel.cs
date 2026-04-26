using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Meshes.UEFormat.Collision;
using CUE4Parse_Conversion.UEFormat;
using CUE4Parse_Conversion.UEFormat.Structs;
using CUE4Parse_Conversion.V2.Dto;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.UEFormat;

public sealed class UEModel : UEFormatExport
{
    protected override string Identifier => "UEMODEL";

    public UEModel(string name, StaticMesh mesh, ExporterOptions options) : base(name, options)
    {
        using (var lodChunk = new FDataChunk("LODS"))
        {
            for (var lodIdx = 0; lodIdx < mesh.LODs.Count; lodIdx++)
            {
                var lod = mesh.LODs[lodIdx];
                using var subLodChunk = new FStaticDataChunk($"LOD{lodIdx}");
                SerializeCommonMeshData(subLodChunk, lod);
                subLodChunk.Serialize(lodChunk);

                lodChunk.Count++;

                if (options.LodFormat == ELodFormat.FirstLod) break;
            }

            lodChunk.Serialize(Ar);
        }

        if (mesh.BodySetup?.TryLoad<UBodySetup>(out var bodySetup) == true && bodySetup.AggGeom?.ConvexElems is { } convexElems)
        {
            using var collisionChunk = new FDataChunk("COLLISION", convexElems.Length);
            foreach (var convexElem in convexElems)
            {
                var collision = new FConvexMeshCollision(convexElem);
                collision.Serialize(collisionChunk);
            }
            collisionChunk.Serialize(Ar);
        }

    }

    public UEModel(string name, Skeleton skeleton, ExporterOptions options) : base(name, options)
    {
        using (var skeletonChunk = new FDataChunk("SKELETON", 1))
        {
            SerializeSkeletonData(skeletonChunk, skeleton);
            skeletonChunk.Serialize(Ar);
        }
    }

    public UEModel(string name, SkeletalMesh mesh, ExporterOptions options) : base(name, options)
    {
        if (mesh.LODs.Count > 0)
        {
            using var lodChunk = new FDataChunk("LODS");
            for (var lodIdx = 0; lodIdx < mesh.LODs.Count; lodIdx++)
            {
                var lod = mesh.LODs[lodIdx];
                using var subLodChunk = new FStaticDataChunk($"LOD{lodIdx}");
                SerializeCommonMeshData(subLodChunk, lod);
                SerializeSkeletalMeshData(subLodChunk, mesh, lodIdx);
                subLodChunk.Serialize(lodChunk);

                lodChunk.Count++;

                if (options.LodFormat == ELodFormat.FirstLod) break;
            }

            lodChunk.Serialize(Ar);
        }

        using (var skeletonChunk = new FDataChunk("SKELETON", 1))
        {
            SerializeSkeletonData(skeletonChunk, mesh);
            skeletonChunk.Serialize(Ar);
        }

        // if (mesh.PhysicsAsset?.TryLoad<UPhysicsAsset>(out var physicsAsset) == true)
        // {
        //     using var physicsChunk = new FDataChunk("PHYSICS", 1);
        //     SerializePhysicsData(physicsChunk, physicsAsset);
        //     physicsChunk.Serialize(Ar);
        // }
    }

    private void SerializeCommonMeshData<TVertex>(FArchiveWriter archive, MeshLod<TVertex> lod) where TVertex : struct, IMeshVertex
    {
        var vertexCount = lod.Vertices.Length;
        using var vertexChunk = new FDataChunk("VERTICES", vertexCount);
        using var normalsChunk = new FDataChunk("NORMALS", vertexCount);
        using var tangentsChunk = new FDataChunk("TANGENTS", vertexCount);

        var mainUvs = new List<FMeshUVFloat>();
        foreach (var vertex in lod.Vertices)
        {
            var position = vertex.Position;
            position.Serialize(vertexChunk);

            var normalSign = vertex.Normal.W;
            normalsChunk.Write(normalSign); // EUEFormatVersion.SerializeBinormalSign

            var normal = (FVector) vertex.Normal;
            normal /= MathF.Sqrt(normal | normal);
            normal.Serialize(normalsChunk);

            var tangent = (FVector) vertex.Tangent;
            tangent.Normalize();
            tangent.Serialize(tangentsChunk);

            var uv = vertex.Uv;
            mainUvs.Add(uv);
        }

        vertexChunk.Serialize(archive);
        normalsChunk.Serialize(archive);
        tangentsChunk.Serialize(archive);

        using (var texCoordsChunk = new FDataChunk("TEXCOORDS"))
        {
            void SerializeUvSet(IEnumerable<FMeshUVFloat> uvSet)
            {
                texCoordsChunk.WriteArray(uvSet, uv => uv.Serialize(texCoordsChunk));
                texCoordsChunk.Count++;
            }

            SerializeUvSet(mainUvs);
            foreach (var extraUvs in lod.ExtraUvs)
            {
                SerializeUvSet(extraUvs);
            }

            texCoordsChunk.Serialize(archive);
        }

        using (var indexChunk = new FDataChunk("INDICES", lod.Indices.Length))
        {
            for (var i = 0; i < lod.Indices.Length; i++)
            {
                indexChunk.Write(lod.Indices[i]);
            }
            indexChunk.Serialize(archive);
        }

        if (lod.VertexColors is { Count: > 0 })
        {
            using var vertexColorChunk = new FDataChunk("VERTEXCOLORS");
            foreach (var vertexColor in lod.VertexColors)
            {
                vertexColorChunk.WriteFString(vertexColor.Key);
                vertexColorChunk.WriteArray(vertexColor.Value, (writer, color) => color.Serialize(writer));
                vertexColorChunk.Count++;
            }

            vertexColorChunk.Serialize(archive);
        }

        using (var materialChunk = new FDataChunk("MATERIALS", lod.Sections.Length))
        {
            for (var i = 0; i < lod.Sections.Length; i++)
            {
                var section = lod.Sections[i];
                var material = lod.Owner.GetMaterial(section);

                materialChunk.WriteFString(material?.SlotName ?? $"MaterialSlot_{i}");
                materialChunk.WriteFString(material?.Material?.ResolvedObject?.GetPathName() ?? string.Empty);
                materialChunk.Write(section.FirstIndex);
                materialChunk.Write(section.NumFaces);
            }

            materialChunk.Serialize(archive);
        }
    }

    private void SerializeSkeletalMeshData(FArchiveWriter archive, SkeletalMesh mesh, int lodIndex)
    {
        using (var weightsChunk = new FDataChunk("WEIGHTS"))
        {
            for (var vertexIndex = 0; vertexIndex < mesh.LODs[lodIndex].Vertices.Length; vertexIndex++)
            {
                var vert = mesh.LODs[lodIndex].Vertices[vertexIndex];

                foreach (var influence in vert.Influences)
                {
                    weightsChunk.Write(influence.Bone);
                    weightsChunk.Write(vertexIndex);
                    weightsChunk.Write(influence.Weight);
                    weightsChunk.Count++;
                }
            }

            weightsChunk.Serialize(archive);
        }

        if (mesh.MorphTargets is { Length: > 0 })
        {
            using var morphTargetsChunk = new FDataChunk("MORPHTARGETS");
            foreach (var morphTarget in mesh.MorphTargets)
            {
                var morph = morphTarget.Load<UMorphTarget>();
                if (morph?.MorphLODModels is null || lodIndex >= morph.MorphLODModels.Length) continue;

                var morphLod = morph.MorphLODModels[lodIndex];

                var morphData = new FMorphTarget(morph.Name, morphLod);
                morphData.Serialize(morphTargetsChunk);
                morphTargetsChunk.Count++;
            }
            morphTargetsChunk.Serialize(archive);
        }
    }

    private void SerializeSkeletonData(FArchiveWriter archive, Skeleton skeleton)
    {
        using (var metaDataChunk = new FDataChunk("METADATA", 1))
        {
            metaDataChunk.WriteFString(skeleton.SkeletonPathName ?? "Skeleton");
            metaDataChunk.Serialize(archive);
        }

        using (var boneChunk = new FDataChunk("BONES", skeleton.RefSkeleton.Count))
        {
            foreach (var bone in skeleton.RefSkeleton)
            {
                var boneName = new FString(bone.Name);
                boneName.Serialize(boneChunk);

                boneChunk.Write(bone.ParentIndex);

                var bonePos = bone.Transform.Translation;
                bonePos.Serialize(boneChunk);

                var boneRot = bone.Transform.Rotation;
                boneRot.Serialize(boneChunk);
            }
            boneChunk.Serialize(archive);
        }

        if (skeleton.Sockets is { Length: > 0 })
        {
            using var socketChunk = new FDataChunk("SOCKETS", skeleton.Sockets.Length);
            foreach (var socketObject in skeleton.Sockets)
            {
                var socket = socketObject.Load<USkeletalMeshSocket>();
                if (socket is null) continue;

                socketChunk.WriteFString(socket.SocketName.Text);
                socketChunk.WriteFString(socket.BoneName.Text);

                var bonePos = socket.RelativeLocation;
                bonePos.Serialize(socketChunk);

                var boneRot = socket.RelativeRotation.Quaternion();
                boneRot.Serialize(socketChunk);

                var boneScale = socket.RelativeScale;
                boneScale.Serialize(socketChunk);
            }

            socketChunk.Serialize(archive);
        }

        if (skeleton.VirtualBones is { Length: > 0 })
        {
            using var virtualBoneChunk = new FDataChunk("VIRTUALBONES", skeleton.VirtualBones.Length);
            foreach (var virtualBone in skeleton.VirtualBones)
            {
                virtualBoneChunk.WriteFString(virtualBone.SourceBoneName.Text);
                virtualBoneChunk.WriteFString(virtualBone.TargetBoneName.Text);
                virtualBoneChunk.WriteFString(virtualBone.VirtualBoneName.Text);
            }

            virtualBoneChunk.Serialize(archive);
        }
    }

    private void SerializePhysicsData(FArchiveWriter archive, UPhysicsAsset physicsAsset)
    {
        using var bodyChunk = new FDataChunk("BODIES", physicsAsset.SkeletalBodySetups.Length);

        foreach (var bodySetupLazy in physicsAsset.SkeletalBodySetups)
        {
            if (!bodySetupLazy.TryLoad<USkeletalBodySetup>(out var bodySetup)) continue;

            var exportBodySetup = new FBodySetup(bodySetup);
            exportBodySetup.Serialize(bodyChunk);
        }

        bodyChunk.Serialize(archive);
    }

}
