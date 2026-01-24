using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Surface;

[StructLayout(LayoutKind.Sequential)]
public struct FSurfaceSubMesh
{
    public int VertexBegin;
    public int VertexEnd;
    public int IndexBegin;
    public int IndexEnd;
    public uint ExternalId;
}