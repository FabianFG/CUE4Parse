using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Chaos.Convex;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FHalfEdgeData<T>
{
    public readonly T PlaneIndex;
    public readonly T VertexIndex;
    public readonly T TwinHalfEdgeIndex;

    public override string ToString() => $"PlaneIndex: {PlaneIndex} VertexIndex: {VertexIndex} TwinHalfEdgeIndex: {TwinHalfEdgeIndex}";
}
