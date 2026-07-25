using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

[StructLayout(LayoutKind.Sequential)]
public struct FGeometryCollectionMeshElement
{
    public short TransformIndex;
    public byte MaterialIndex;
    public byte bIsInternal;
    public uint TriangleStart;
    public uint TriangleCount;
    public uint VertexStart;
    public uint VertexEnd;
}