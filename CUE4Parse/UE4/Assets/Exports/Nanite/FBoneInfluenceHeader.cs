using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Nanite;

public record struct FClusterBoneInfluence(uint BoneIndex);

public record struct FBoneInfluence(uint BoneIndex, float BoneWeight);

public readonly struct FBoneInfluenceHeader
{
    public readonly uint DataAddress;
    public readonly int NumVertexBoneInfluences;
    public readonly int NumVertexBoneIndexBits;
    public readonly int NumVertexBoneWeightBits;

    public FBoneInfluenceHeader(FArchive Ar, long pageBaseAddress)
    {
        var packed = Ar.Read<TIntVector2<uint>>();
        DataAddress = (uint)pageBaseAddress + NaniteUtils.GetBits(packed.X, 22, 0);
        NumVertexBoneInfluences = (int)NaniteUtils.GetBits(packed.X, 10, 22);
        NumVertexBoneIndexBits = (int)NaniteUtils.GetBits(packed.Y, 6, 0);
        NumVertexBoneWeightBits = (int)NaniteUtils.GetBits(packed.Y, 5, 6);
    }
};
