using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FGPUVertFloat : FSkelMeshVertexBase
{
    private const int MAX_SKELETAL_UV_SETS_UE4 = 4;
    public FMeshUVFloat[] UV;

    public FGPUVertFloat()
    {
        UV = [];
    }

    public FGPUVertFloat(FArchive Ar, bool bExtraBoneInfluences, int numSkelUVSets) : this()
    {
        SerializeForGPU(Ar, bExtraBoneInfluences);

        UV = new FMeshUVFloat[MAX_SKELETAL_UV_SETS_UE4];
        for (var i = 0; i < numSkelUVSets; i++)
        {
            UV[i] = Ar.Read<FMeshUVFloat>();
        }
    }
}
