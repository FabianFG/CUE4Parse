using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Chaos.Convex;

[StructLayout(LayoutKind.Sequential)]
public struct FPlaneData<T>
{
    public T FirstHalfEdgeIndex;
    public T NumHalfEdges;

    public override string ToString() => $"FirstHalfEdgeIndex: {FirstHalfEdgeIndex} NumHalfEdges: {NumHalfEdges}";
}