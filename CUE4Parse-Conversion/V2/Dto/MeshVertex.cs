using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;

namespace CUE4Parse_Conversion.V2.Dto;

public interface IMeshVertex
{
    public FVector Position { get; }
    public FVector4 Normal { get; }
    public FVector4 Tangent { get; }
    public FMeshUVFloat Uv { get; }
}

public readonly struct MeshVertex : IMeshVertex
{
    public FVector Position { get; } = FVector.ZeroVector;
    public FVector4 Normal { get; } = FVector4.ZeroVector;
    public FVector4 Tangent { get; } = FVector4.ZeroVector;
    public FMeshUVFloat Uv { get; } = FMeshUVFloat.ZeroVector;

    private MeshVertex(FVector position, FVector4 normal, FVector4 tangent, FMeshUVFloat uv)
    {
        Position = position;
        Normal = normal;
        Tangent = tangent;
        Uv = uv;
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

public readonly struct SkinnedMeshVertex : IMeshVertex
{
    public FVector Position { get; } = FVector.ZeroVector;
    public FVector4 Normal { get; } = FVector4.ZeroVector;
    public FVector4 Tangent { get; } = FVector4.ZeroVector;
    public FMeshUVFloat Uv { get; } = FMeshUVFloat.ZeroVector;
    public MeshBoneInfluence[] Influences { get; } = [];

    private SkinnedMeshVertex(FVector position, FVector4 normal, FVector4 tangent, FMeshUVFloat uv)
    {
        Position = position;
        Normal = normal;
        Tangent = tangent;
        Uv = uv;
    }

    public SkinnedMeshVertex(FSkelMeshVertexBase vertex, ushort[] boneMap) : this(vertex.Pos, vertex.Normal[2], vertex.Normal[0], vertex.UVs[0])
    {
        if (vertex.Infs == null) return;

        var count = 0;
        foreach (var weight in vertex.Infs.BoneWeight)
        {
            if (weight != 0)
                count++;
        }
        if (count == 0) return;

        var scale = vertex.Infs.bUse16BitBoneWeight ? Constants.UShort_Bone_Scale : Constants.Byte_Bone_Scale;
        var influences = new MeshBoneInfluence[count];
        var idx = 0;
        for (var i = 0; i < vertex.Infs.BoneWeight.Length; i++)
        {
            var weight = vertex.Infs.BoneWeight[i];
            if (weight == 0) continue;

            influences[idx++] = new MeshBoneInfluence(boneMap[vertex.Infs.BoneIndex[i]], weight, weight * scale);
        }

        Influences = idx == count ? influences : influences[..idx];
    }
}
