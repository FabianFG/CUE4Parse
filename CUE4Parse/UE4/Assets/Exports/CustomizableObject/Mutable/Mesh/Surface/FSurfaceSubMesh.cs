using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Surface;

[StructLayout(LayoutKind.Sequential)]
public struct FSurfaceSubMesh
{
    public int VertexBegin;
    public int VertexEnd;
    public int IndexBegin;
    public int IndexEnd;
    public uint ExternalId;

    public FSurfaceSubMesh(FMutableArchive Ar)
    {
        VertexBegin = Ar.Read<int>();
        VertexEnd = Ar.Read<int>();
        IndexBegin = Ar.Read<int>();
        IndexEnd = Ar.Read<int>();
        ExternalId = Ar.Game >= EGame.GAME_UE5_8 ? 0 : Ar.Read<uint>();
    }
}
