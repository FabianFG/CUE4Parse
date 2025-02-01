using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Surfaces;

public class FSurfaceSubMesh
{
    public int VertexBegin;
    public int VertexEnd;
    public int IndexBegin;
    public int IndexEnd;
    public uint ExternalId;

    public FSurfaceSubMesh(FArchive Ar)
    {
        VertexBegin = Ar.Read<int>();
        VertexEnd = Ar.Read<int>();
        IndexBegin = Ar.Read<int>();
        IndexEnd = Ar.Read<int>();
        ExternalId = Ar.Read<uint>();
    }
}