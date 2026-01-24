using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Surface;

public class FMeshSurface
{
    public FSurfaceSubMesh[] SubMeshes;
    public uint BoneMapIndex;
    public uint BoneMapCount;
    public uint Id;
    
    public FMeshSurface(FMutableArchive Ar)
    {
        SubMeshes = Ar.ReadArray<FSurfaceSubMesh>();
        BoneMapIndex = Ar.Read<uint>();
        BoneMapCount = Ar.Read<uint>();
        Id = Ar.Read<uint>();
    }
}