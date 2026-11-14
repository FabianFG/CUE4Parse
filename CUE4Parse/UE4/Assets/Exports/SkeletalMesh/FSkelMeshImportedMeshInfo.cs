using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

public struct FSkelMeshImportedMeshInfo
{
    public FName Name;
    public int NumVertices;
    public int StartImportedVertex;

    public FSkelMeshImportedMeshInfo(FAssetArchive Ar)
    {
        Name = Ar.ReadFName();
        NumVertices = Ar.Read<int>();
        StartImportedVertex = Ar.Read<int>();
    }
}
