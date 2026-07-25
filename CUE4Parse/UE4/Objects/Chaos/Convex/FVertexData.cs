namespace CUE4Parse.UE4.Objects.Chaos.Convex;

public struct FVertexData<T>
{
    public T FirstHalfEdgeIndex;

    public override string ToString() => $"FirstHalfEdgeIndex: {FirstHalfEdgeIndex}";
}