using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Chaos.Convex;

public class TConvexHalfEdgeStructureData<T> where T : unmanaged
{
    public FPlaneData<T>[] Planes;
    public FHalfEdgeData<T>[] HalfEdges;
    public FVertexData<T>[] Vertices;
    public T[]? Edges;

    public TConvexHalfEdgeStructureData(FChaosArchive Ar)
    {
        Planes = Ar.ReadArray<FPlaneData<T>>();
        HalfEdges = Ar.ReadArray<FHalfEdgeData<T>>();
        Vertices = Ar.ReadArray<FVertexData<T>>();

        if (FPhysicsObjectVersion.Get(Ar) >= FPhysicsObjectVersion.Type.ChaosConvexHasUniqueEdgeSet)
            Edges = Ar.ReadArray<T>();
    }
}