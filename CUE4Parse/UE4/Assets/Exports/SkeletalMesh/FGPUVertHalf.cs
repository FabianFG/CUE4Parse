using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FGPUVertHalf : FSkelMeshVertexBase
{
    public sealed override FMeshUVFloat[] UVs { get; }

    public FGPUVertHalf()
    {

    }

    public FGPUVertHalf(FArchive Ar, bool bExtraBoneInfluences, int numSkelUVSets) : this()
    {
        SerializeForGPU(Ar, bExtraBoneInfluences);
        var uvs = Ar.ReadArray<FMeshUVHalf>(numSkelUVSets);

        UVs = new FMeshUVFloat[uvs.Length];
        for (var i = 0; i < uvs.Length; i++)
        {
            UVs[i] = (FMeshUVFloat) uvs[i];
        }
    }
}
