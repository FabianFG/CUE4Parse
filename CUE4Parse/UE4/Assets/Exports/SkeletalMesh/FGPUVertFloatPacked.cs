using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FGPUVertFloatPacked : FSkelMeshVertexBase
{
    public FVectorIntervalFixed32GPU Pos;
    public override FMeshUVFloat[] UVs { get; }

    public FGPUVertFloatPacked()
    {
        UVs = [];
    }

    public FGPUVertFloatPacked(FArchive Ar, int numSkelUVSets) : this()
    {
        SerializeForGPU(Ar);

        Pos = new FVectorIntervalFixed32GPU(Ar);
        UVs = Ar.ReadArray<FMeshUVFloat>(numSkelUVSets);
    }

}
