using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Chaos.Convex;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FPlaneData<T>
{
    public readonly T FirstHalfEdgeIndex;
    public readonly T NumHalfEdges;

    public override string ToString() => $"FirstHalfEdgeIndex: {FirstHalfEdgeIndex} NumHalfEdges: {NumHalfEdges}";
}
