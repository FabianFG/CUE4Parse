using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FGeometryCollectionMeshElement
{
    public readonly short TransformIndex;
    public readonly byte MaterialIndex;
    public readonly byte bIsInternal;
    public readonly uint TriangleStart;
    public readonly uint TriangleCount;
    public readonly uint VertexStart;
    public readonly uint VertexEnd;
}
