using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FGPUVertHalfPacked : FSkelMeshVertexBase
{
    public FVectorIntervalFixed32GPU Pos;
    public readonly FMeshUVHalf[] UV;

    public FGPUVertHalfPacked()
    {
        UV = [];
    }
    public FGPUVertHalfPacked(FArchive Ar, int numSkelUVSets) : this()
    {
        SerializeForGPU(Ar);

        Pos = new FVectorIntervalFixed32GPU(Ar);
        UV = Ar.ReadArray<FMeshUVHalf>(numSkelUVSets);
    }
}
