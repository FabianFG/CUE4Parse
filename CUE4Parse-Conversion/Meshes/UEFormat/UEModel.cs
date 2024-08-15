using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Meshes.UEFormat.Collision;
using CUE4Parse_Conversion.UEFormat;
using CUE4Parse_Conversion.UEFormat.Structs;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.UEFormat;

public class UEModel : UEFormatExport
{
    protected override string Identifier { get; set; } = "UEMODEL";
    
    public UEModel(string name, CStaticMesh mesh, FPackageIndex bodySetupLazy, ExporterOptions options) : base(name, options) 
    {
        using (var lodChunk = new FDataChunk("LODS"))
        {
            for (var lodIdx = 0; lodIdx < mesh.LODs.Count; lodIdx++)
            {
                var lod = mesh.LODs[lodIdx];
                if (lod.SkipLod) continue;

                using var subLodChunk = new FStaticDataChunk($"LOD{lodIdx}");
                SerializeStaticMeshData(subLodChunk, lod.Verts, lod.Indices.Value, lod.VertexColors, lod.ExtraVertexColors, lod.Sections.Value, lod.ExtraUV.Value);
                subLodChunk.Serialize(lodChunk);
            
                lodChunk.Count++;
                
                if (options.LodFormat == ELodFormat.FirstLod) break;
            }
        
            lodChunk.Serialize(Ar);
        }
        
        if (bodySetupLazy.TryLoad<UBodySetup>(out var bodySetup) && bodySetup.AggGeom?.ConvexElems is { } convexElems)
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
    
    public UEModel(string name, CSkeletalMesh mesh, FPackageIndex[]? morphTargets, FPackageIndex[] sockets, FPackageIndex physicsAssetLazy, ExporterOptions options) : base(name, options)
    {
        using (var lodChunk = new FDataChunk("LODS"))
        {
            for (var lodIdx = 0; lodIdx < mesh.LODs.Count; lodIdx++)
            {
                var lod = mesh.LODs[lodIdx];
                if (lod.SkipLod) continue;

                using var subLodChunk = new FStaticDataChunk($"LOD{lodIdx}");
                SerializeStaticMeshData(subLodChunk, lod.Verts, lod.Indices.Value, lod.VertexColors, lod.ExtraVertexColors, lod.Sections.Value, lod.ExtraUV.Value);
                SerializeSkeletalMeshData(subLodChunk, lod.Verts, morphTargets, lodIdx);
                subLodChunk.Serialize(lodChunk);
            
                lodChunk.Count++;
                
                if (options.LodFormat == ELodFormat.FirstLod) break;
            }
        
            lodChunk.Serialize(Ar);
        }

        using (var skeletonChunk = new FDataChunk("SKELETON", 1))
        {
            SerializeSkeletonData(skeletonChunk, mesh.RefSkeleton, sockets, []);
            
            skeletonChunk.Serialize(Ar);
        }

        /*if (physicsAssetLazy.TryLoad(out UPhysicsAsset physicsAsset))
        {
            using var physicsChunk = new FDataChunk("PHYSICS", 1);

            SerializePhysicsData(physicsChunk, physicsAsset);
            
            physicsChunk.Serialize(Ar);
        }*/
    }
    
    public UEModel(string name, List<CSkelMeshBone> bones, FPackageIndex[] sockets, FVirtualBone[] virtualBones, ExporterOptions options) : base(name, options)
    {
        using (var skeletonChunk = new FDataChunk("SKELETON", 1))
        {
            SerializeSkeletonData(skeletonChunk, bones, sockets, virtualBones);
            
            skeletonChunk.Serialize(Ar);
        }
    }

    private void SerializeStaticMeshData(FArchiveWriter archive, IReadOnlyCollection<CMeshVertex> verts, FRawStaticIndexBuffer indices, FColor[]? vertexColors, CVertexColor[]? extraVertexColors, CMeshSection[] sections, FMeshUVFloat[][] extraUVs)
    {
        using var vertexChunk = new FDataChunk("VERTICES", verts.Count);
        using var normalsChunk = new FDataChunk("NORMALS", verts.Count);
        using var tangentsChunk = new FDataChunk("TANGENTS", verts.Count);

        var mainUVs = new List<FMeshUVFloat>();
        foreach (var vert in verts)
        {
            var position = vert.Position;
            position.Y = -position.Y;
            position.Serialize(vertexChunk);

            var normalSign = vert.Normal.W;
            normalsChunk.Write(normalSign); // EUEFormatVersion.SerializeBinormalSign
            
            var normal = (FVector) vert.Normal;
            normal /= MathF.Sqrt(normal | normal);
            normal.Y = -normal.Y;
            normal.Serialize(normalsChunk);
            
            var tangent = (FVector) vert.Tangent;
            tangent.Normalize();
            tangent.Y = -tangent.Y;
            tangent.Serialize(tangentsChunk);
            
            var uv = vert.UV;
            mainUVs.Add(uv);
        }
        
        vertexChunk.Serialize(archive);
        normalsChunk.Serialize(archive);
        tangentsChunk.Serialize(archive);
        
        using (var texCoordsChunk = new FDataChunk("TEXCOORDS"))
        {
            void SerializeUVSet(IEnumerable<FMeshUVFloat> uvSet)
            {
                texCoordsChunk.WriteArray(uvSet, uv =>
                {
                    uv.V = 1 - uv.V;
                    uv.Serialize(texCoordsChunk);
                });
                
                texCoordsChunk.Count++;
            }

            SerializeUVSet(mainUVs);
            foreach (var extraUVSet in extraUVs)
            {
                SerializeUVSet(extraUVSet);
            }
            
            texCoordsChunk.Serialize(archive);
        }
        
        using (var indexChunk = new FDataChunk("INDICES", indices.Length))
        {
            for (var i = 0; i < indices.Length; i++)
            {
                indexChunk.Write(indices[i]);
            }
            
            indexChunk.Serialize(archive);
        }
        
        if (vertexColors?.Length > 0 || extraVertexColors?.Length > 0)
        {
            using var vertexColorChunk = new FDataChunk("VERTEXCOLORS");
            if (vertexColors is not null)
            {
                vertexColorChunk.WriteFString("COL0"); // todo fallback to default name w/ index for other extras, maybe just combine extra vtx col and main?
                vertexColorChunk.WriteArray(vertexColors, (writer, color) => color.Serialize(writer));
                vertexColorChunk.Count++;
            }
            
            if (extraVertexColors is not null)
            {
                foreach (var extraVertexColor in extraVertexColors)
                {
                    vertexColorChunk.WriteFString(extraVertexColor.Name);
                    vertexColorChunk.WriteArray(extraVertexColor.ColorData, (writer, color) => color.Serialize(writer));
                    vertexColorChunk.Count++;
                }
            }
           
            vertexColorChunk.Serialize(archive);
        }
        
        using (var materialChunk = new FDataChunk("MATERIALS", sections.Length))
        {
            foreach (var section in sections)
            {
                var materialName = section.Material?.Name.Text ?? string.Empty;
                materialChunk.WriteFString(materialName);
                materialChunk.Write(section.FirstIndex);
                materialChunk.Write(section.NumFaces);
            }

            materialChunk.Serialize(archive);
        }
    }

    private void SerializeSkeletalMeshData(FArchiveWriter archive, CSkelMeshVertex[] verts, FPackageIndex[]? morphTargets, int lodIndex)
    {
        using (var weightsChunk = new FDataChunk("WEIGHTS"))
        {
            for (var vertexIndex = 0; vertexIndex < verts.Length; vertexIndex++)
            {
                var vert = verts[vertexIndex];
                
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
        
        if (morphTargets is {Length: > 0})
        {
            using var morphTargetsChunk = new FDataChunk("MORPHTARGETS", morphTargets.Length);
            foreach (var morphTarget in morphTargets)
            {
                var morph = morphTarget.Load<UMorphTarget>();
                if (morph?.MorphLODModels is null || lodIndex >= morph.MorphLODModels.Length) continue;

                var morphLod = morph.MorphLODModels[lodIndex];
            
                var morphData = new FMorphTarget(morph.Name, morphLod);
                morphData.Serialize(morphTargetsChunk);
            }
            morphTargetsChunk.Serialize(archive);
        }
    }

    private void SerializeSkeletonData(FArchiveWriter archive, List<CSkelMeshBone> bones, FPackageIndex[] sockets, FVirtualBone[] virtualBones)
    {
        using (var boneChunk = new FDataChunk("BONES", bones.Count))
        {
            foreach (var bone in bones)
            {
                var boneName = new FString(bone.Name.Text);
                boneName.Serialize(boneChunk);
            
                boneChunk.Write(bone.ParentIndex);

                var bonePos = bone.Position;
                bonePos.Y = -bonePos.Y;
                bonePos.Serialize(boneChunk);

                var boneRot = bone.Orientation;
                boneRot.Y = -boneRot.Y;
                boneRot.W = -boneRot.W;
                boneRot.Serialize(boneChunk);
            }
            boneChunk.Serialize(archive);
        }
        
        using (var socketChunk = new FDataChunk("SOCKETS", sockets.Length))
        {
            foreach (var socketObject in sockets)
            {
                var socket = socketObject.Load<USkeletalMeshSocket>();
                if (socket is null) continue;

                socketChunk.WriteFString(socket.SocketName.Text);
                socketChunk.WriteFString(socket.BoneName.Text);
            
                var bonePos = socket.RelativeLocation;
                bonePos.Y = -bonePos.Y;
                bonePos.Serialize(socketChunk);

                var boneRot = socket.RelativeRotation.Quaternion();
                boneRot.Y = -boneRot.Y;
                boneRot.W = -boneRot.W;
                boneRot.Serialize(socketChunk);
            
                var boneScale = socket.RelativeScale;
                boneScale.Y = -boneScale.Y;
                boneScale.Serialize(socketChunk);
            }

            socketChunk.Serialize(archive);
        }
        
        using (var virtualBoneChunk = new FDataChunk("VIRTUALBONES", virtualBones.Length))
        {
            foreach (var virtualBone in virtualBones)
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
        using (var bodyChunk = new FDataChunk("BODIES", physicsAsset.SkeletalBodySetups.Length))
        {
            foreach (var bodySetupLazy in physicsAsset.SkeletalBodySetups)
            {
                if (!bodySetupLazy.TryLoad<USkeletalBodySetup>(out var bodySetup)) continue;
                
                var exportBodySetup = new FBodySetup(bodySetup);
                exportBodySetup.Serialize(bodyChunk);
            }
            
            bodyChunk.Serialize(archive);
        }
    }

}