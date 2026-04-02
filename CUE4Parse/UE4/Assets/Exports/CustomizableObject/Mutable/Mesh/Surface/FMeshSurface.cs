using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Surface;

public class FMeshSurface
{
    public int Version = 1;
    public FSurfaceSubMesh[] SubMeshes;
    public uint BoneMapIndex;
    public uint BoneMapCount;
    public uint Id;

    public FMeshSurfaceLegacy Surface_Deprecated;

    public FMeshSurface(FMutableArchive Ar)
    {
        if (Ar.Game >= EGame.GAME_UE5_5)
        {
            SubMeshes = Ar.ReadArray<FSurfaceSubMesh>();
            BoneMapIndex = Ar.Read<uint>();
            BoneMapCount = Ar.Read<uint>();
            Id = Ar.Read<uint>();
        }
        else
        {
            Version = Ar.Read<int>();
            Surface_Deprecated = new FMeshSurfaceLegacy(Ar, false);
            BoneMapIndex = Ar.Read<uint>();
            BoneMapCount = Ar.Read<uint>();
            if (Version >= 1)
            {
                var bCastShadow = Ar.ReadBoolean();
            }
            Id = Ar.Read<uint>();
        }
    }
}

public struct FMeshSurfaceLegacy
{
    public int FirstVertex;
    public int VertexCount;
    public int FirstIndex;
    public int IndexCount;
    public uint Id;

    public FMeshSurfaceLegacy(FMutableArchive Ar, bool readId)
    {
        FirstVertex = Ar.Read<int>();
        VertexCount = Ar.Read<int>();
        FirstIndex = Ar.Read<int>();
        IndexCount = Ar.Read<int>();
        if (readId)
            Id = Ar.Read<uint>();
    }
}
