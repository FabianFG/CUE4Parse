using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Chaos.Convex;

[StructLayout(LayoutKind.Sequential)]
public struct FHalfEdgeData<T>
{
    public T PlaneIndex;
    public T VertexIndex;
    public T TwinHalfEdgeIndex;

    public override string ToString() => $"PlaneIndex: {PlaneIndex} VertexIndex: {VertexIndex} TwinHalfEdgeIndex: {TwinHalfEdgeIndex}";
}