using System.Runtime.InteropServices;

namespace CUE4Parse.UE4.Objects.Chaos.Convex;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FVertexData<T>
{
    public readonly T FirstHalfEdgeIndex;

    public override string ToString() => $"FirstHalfEdgeIndex: {FirstHalfEdgeIndex}";
}
