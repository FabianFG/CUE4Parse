using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FGPUVertHalf : FSkelMeshVertexBase
{
    public readonly FMeshUVHalf[] UV;

    public FGPUVertHalf()
    {
        UV = [];
    }

    public FGPUVertHalf(FArchive Ar, bool bExtraBoneInfluences, int numSkelUVSets) : this()
    {
        SerializeForGPU(Ar, bExtraBoneInfluences);
        UV = Ar.ReadArray<FMeshUVHalf>(numSkelUVSets);
    }
}
