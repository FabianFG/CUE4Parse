using System;
using System.Collections.Generic;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.UEFormat;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Meshes.UEFormat;

public class UEModel : UEFormatExport
{
    protected override string Identifier { get; set; } = "UEMODEL";
    
    public UEModel(string name, CStaticMeshLod lod, ExporterOptions options) : base(name, options) 
    {
        SerializeStaticMeshData(lod.Verts, lod.Indices.Value, lod.VertexColors, lod.Sections.Value, lod.ExtraUV.Value);
    }
    
    public UEModel(string name, CSkelMeshLod lod, List<CSkelMeshBone> bones, FPackageIndex[]? morphTargets, FPackageIndex[] sockets, int lodIndex, ExporterOptions options) : base(name, options)
    {
        SerializeStaticMeshData(lod.Verts, lod.Indices.Value, lod.VertexColors, lod.Sections.Value, lod.ExtraUV.Value);
        SerializeSkeletalMeshData(lod.Verts, morphTargets, lodIndex);
        SerializeSkeletonData(bones, sockets);
    }
    
    public UEModel(string name, List<CSkelMeshBone> bones, FPackageIndex[] sockets, ExporterOptions options) : base(name, options)
    {
        SerializeSkeletonData(bones, sockets);
    }

    private void SerializeStaticMeshData(IReadOnlyCollection<CMeshVertex> verts, FRawStaticIndexBuffer indices, FColor[]? vertexColors, CMeshSection[] sections, FMeshUVFloat[][] extraUVs)
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
        
        vertexChunk.Serialize(Ar);
        normalsChunk.Serialize(Ar);
        tangentsChunk.Serialize(Ar);
        
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
            
            texCoordsChunk.Serialize(Ar);
        }
        
        using (var indexChunk = new FDataChunk("INDICES", indices.Length))
        {
            for (var i = 0; i < indices.Length; i++)
            {
                indexChunk.Write(indices[i]);
            }
            
            indexChunk.Serialize(Ar);
        }
        
        if (vertexColors is not null)
        {
            using var vertexColorChunk = new FDataChunk("VERTEXCOLORS", vertexColors.Length);
            for (var i = 0; i < vertexColors.Length; i++)
            {
                vertexColors[i].Serialize(vertexColorChunk);
            }
            vertexColorChunk.Serialize(Ar);
        }
        
        using (var materialChunk = new FDataChunk("MATERIALS", sections.Length))
        {
            foreach (var section in sections)
            {
                var materialName = section.Material?.Load<UMaterialInterface>()?.Name ?? string.Empty;
                materialChunk.WriteFString(materialName);
                materialChunk.Write(section.FirstIndex);
                materialChunk.Write(section.NumFaces);
            }

            materialChunk.Serialize(Ar);
        }
    }

    private void SerializeSkeletalMeshData(CSkelMeshVertex[] verts, FPackageIndex[]? morphTargets, int lodIndex)
    {
        // TODO can we work on getting unlimited influences, no more psk restrictions !!
        using (var weightsChunk = new FDataChunk("WEIGHTS"))
        {
            for (var vertexIndex = 0; vertexIndex < verts.Length; vertexIndex++)
            {
                var vert = verts[vertexIndex];
            
                var vertBones = vert.Bone;
                if (vertBones is null) continue;
            
                var weights = vert.UnpackWeights();
                for (var index = 0; index < weights.Length; index++)
                {
                    var weight = weights[index];
                    if (weight <= 0) continue;
                    
                    weightsChunk.Write(vertBones[index]);
                    weightsChunk.Write(vertexIndex);
                    weightsChunk.Write(weight);
                    weightsChunk.Count++;
                }
            }
        
            weightsChunk.Serialize(Ar);
        }
        
        if (morphTargets is not null)
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
            morphTargetsChunk.Serialize(Ar);
        }
    }

    private void SerializeSkeletonData(List<CSkelMeshBone> bones, FPackageIndex[] sockets)
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
            boneChunk.Serialize(Ar);
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

            socketChunk.Serialize(Ar);
        }
    }

}