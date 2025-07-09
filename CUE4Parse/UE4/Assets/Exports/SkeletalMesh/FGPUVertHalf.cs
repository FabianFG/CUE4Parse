using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FGPUVertHalf : FSkelMeshVertexBase
{
    private const int MAX_SKELETAL_UV_SETS_UE4 = 4;
    public readonly FMeshUVHalf[] UV;

    public FGPUVertHalf()
    {
        UV = [];
    }

    public FGPUVertHalf(FArchive Ar, bool bExtraBoneInfluences, int numSkelUVSets) : this()
    {
        SerializeForGPU(Ar, bExtraBoneInfluences);

        UV = new FMeshUVHalf[MAX_SKELETAL_UV_SETS_UE4];
        for (var i = 0; i < numSkelUVSets; i++)
        {
            UV[i] = Ar.Read<FMeshUVHalf>();
        }
    }
}
