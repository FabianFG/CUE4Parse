using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public class FSkeletalMeshHalfEdgeBuffer
{
    public readonly int[] VertexToEdgeData;
    public readonly int[] EdgeToTwinEdgeData;

    public FSkeletalMeshHalfEdgeBuffer(FArchive Ar)
    {
        VertexToEdgeData = Ar.ReadArray<int>();
        EdgeToTwinEdgeData = Ar.ReadArray<int>();
    }
}
