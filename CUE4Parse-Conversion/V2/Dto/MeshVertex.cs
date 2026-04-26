using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;

namespace CUE4Parse_Conversion.V2.Dto;

public class MeshVertex(FVector position, FVector4 normal, FVector4 tangent, FMeshUVFloat uv)
{
    public readonly FVector Position = position;
    public readonly FVector4 Normal = normal;
    public readonly FVector4 Tangent = tangent;
    public readonly FMeshUVFloat Uv = uv;

     public MeshVertex() : this(FVector.ZeroVector, FVector4.ZeroVector, FVector4.ZeroVector, new FMeshUVFloat(0, 0))
     {

     }

     public MeshVertex(FVector position, FPackedNormal normal, FPackedNormal tangent, FMeshUVFloat uv) : this(position, (FVector4) normal, (FVector4) tangent, uv)
     {

     }

     public MeshVertex(FVector position, FNaniteVertexAttributes attributes, bool hasTangents) : this(position, new FVector4(attributes.Normal), hasTangents ? attributes.TangentXAndSign : FVector4.ZeroVector, new FMeshUVFloat(attributes.UVs[0].X, attributes.UVs[0].Y))
     {

     }

     public MeshVertex(FVector position, FVector normal, FVector4 tangent, FMeshUVFloat uv) : this(position, new FVector4(normal), tangent, uv)
     {

     }
}

public class SkinnedMeshVertex : MeshVertex
{
    public readonly IReadOnlyList<BoneInfluence> Influences = [];

    public SkinnedMeshVertex()
    {

    }

    public SkinnedMeshVertex(FSkelMeshVertexBase vertex, ushort[] boneMap) : base(vertex.Pos, vertex.Normal[2], vertex.Normal[0], vertex.UVs[0])
    {
        if (vertex.Infs == null) return;

        var influences = new List<BoneInfluence>();
        var scale = vertex.Infs.bUse16BitBoneWeight ? Constants.UShort_Bone_Scale : Constants.Byte_Bone_Scale;
        foreach (var (weight, boneIndex) in vertex.Infs.BoneWeight.Zip(vertex.Infs.BoneIndex))
        {
            if (weight == 0) continue;
            influences.Add(new BoneInfluence(boneMap[boneIndex], weight, weight * scale));
        }

        Influences = influences;
    }
}
