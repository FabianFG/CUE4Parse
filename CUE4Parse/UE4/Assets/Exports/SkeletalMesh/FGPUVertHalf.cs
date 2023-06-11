using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Meshes;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FGPUVertHalf : FSkelMeshVertexBase
{
    private const int MAX_SKELETAL_UV_SETS_UE4 = 4;
    public readonly FMeshUVHalf[] UV;

    public FGPUVertHalf()
    {
        UV = Array.Empty<FMeshUVHalf>();
    }

    public FGPUVertHalf(FAssetArchive Ar, bool bExtraBoneInfluences, int numSkelUVSets) : this()
    {
        SerializeForGPU(Ar, bExtraBoneInfluences);

        UV = new FMeshUVHalf[MAX_SKELETAL_UV_SETS_UE4];
        for (var i = 0; i < numSkelUVSets; i++)
        {
            UV[i] = Ar.Read<FMeshUVHalf>();
        }
    }
}
