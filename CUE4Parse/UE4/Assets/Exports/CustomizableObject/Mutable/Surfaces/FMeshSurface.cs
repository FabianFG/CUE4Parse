using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Surfaces;

public class FMeshSurface
{
    public FSurfaceSubMesh[] SubMeshes;
    public uint BoneMapIndex;
    public uint BoneMapCount;
    public uint Id;
    
    public FMeshSurface(FAssetArchive Ar)
    {
        SubMeshes = Ar.ReadArray(() => new FSurfaceSubMesh(Ar));
        BoneMapIndex = Ar.Read<uint>();
        BoneMapCount = Ar.Read<uint>();
        Id = Ar.Read<uint>();
    }
}