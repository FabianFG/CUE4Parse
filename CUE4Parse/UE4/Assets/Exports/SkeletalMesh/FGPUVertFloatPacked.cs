using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FGPUVertFloatPacked : FSkelMeshVertexBase
{
    public FVectorIntervalFixed32GPU Pos;
    public FMeshUVFloat[] UV;

    public FGPUVertFloatPacked()
    {
        UV = [];
    }

    public FGPUVertFloatPacked(FArchive Ar, int numSkelUVSets) : this()
    {
        SerializeForGPU(Ar);

        Pos = new FVectorIntervalFixed32GPU(Ar);
        UV = Ar.ReadArray<FMeshUVFloat>(numSkelUVSets);
    }
}
