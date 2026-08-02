using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FGPUVertHalfPacked : FSkelMeshVertexBase
{
    public FVectorIntervalFixed32GPU Pos;
    public sealed override FMeshUVFloat[] UVs { get; }

    public FGPUVertHalfPacked()
    {
        UVs = [];
    }

    public FGPUVertHalfPacked(FArchive Ar, int numSkelUVSets) : this()
    {
        SerializeForGPU(Ar);

        Pos = new FVectorIntervalFixed32GPU(Ar);
        var uvs = Ar.ReadArray<FMeshUVHalf>(numSkelUVSets);

        UVs = new FMeshUVFloat[uvs.Length];
        for (var i = 0; i < uvs.Length; i++)
        {
            UVs[i] = (FMeshUVFloat) uvs[i];
        }
    }
}
