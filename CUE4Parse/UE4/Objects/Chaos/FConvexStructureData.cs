using System.Runtime.InteropServices;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using FConvexStructureDataLarge = CUE4Parse.UE4.Objects.Chaos.TConvexHalfEdgeStructureData<int>;
using FConvexStructureDataMedium = CUE4Parse.UE4.Objects.Chaos.TConvexHalfEdgeStructureData<short>;
using FConvexStructureDataSmall = CUE4Parse.UE4.Objects.Chaos.TConvexHalfEdgeStructureData<byte>;

namespace CUE4Parse.UE4.Objects.Chaos;

public enum EIndexType : sbyte
{
    None,
    Small,
    Medium,
    Large
}

[StructLayout(LayoutKind.Sequential)]
public struct FStructureData
{
    public FConvexStructureDataLarge? DataL;
    public FConvexStructureDataMedium? DataM;
    public FConvexStructureDataSmall? DataS;
}

public class FConvexStructureData
{
    public EIndexType IndexType;
    public FStructureData Data;

    public FConvexStructureData(FArchive Ar)
    {
        var bUseHalfEdgeStructureData = FPhysicsObjectVersion.Get(Ar) >= FPhysicsObjectVersion.Type.ChaosConvexUsesHalfEdges;
        if (!bUseHalfEdgeStructureData)
            // LoadLegacyData(Ar);
            // return
            // TODO load legacy data
            throw new NotImplementedException("Loading legacy convex structure data is not implemented");

        IndexType = (EIndexType)Ar.Read<EIndexType>();

        Data = new FStructureData();
        if  (IndexType == EIndexType.Large)
            Data.DataL = new FConvexStructureDataLarge(Ar);
        else if (IndexType == EIndexType.Medium)
            Data.DataM = new FConvexStructureDataMedium(Ar);
        else if (IndexType == EIndexType.Small)
            Data.DataS = new FConvexStructureDataSmall(Ar);

    }
}

public class TConvexHalfEdgeStructureData<T> where T : struct
{
    public T[] Edges;
    public FHalfEdgeData[] HalfEdges;
    public FPlaneData[] Planes;
    public FVertexPlanes[] VertexPlanes;
    public FVertexData[] Vertices;

    public TConvexHalfEdgeStructureData(FArchive Ar)
    {
        Planes = Ar.ReadArray<FPlaneData>();
        HalfEdges = Ar.ReadArray<FHalfEdgeData>();
        Vertices = Ar.ReadArray<FVertexData>();

        var bHasUniqueEdgeList = FPhysicsObjectVersion.Get(Ar) >= FPhysicsObjectVersion.Type.ChaosConvexHasUniqueEdgeSet;
        if (bHasUniqueEdgeList)
            Edges = Ar.ReadArray<T>();
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FPlaneData
    {
        private readonly T FirstHalfEdgeIndex; // index into HalfEdges
        private readonly T NumHalfEdges;
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FHalfEdgeData
    {
        private readonly T PlaneIndex; // index into Planes
        private readonly T VertexIndex; // index into Vertices
        private readonly T TwinHalfEdgeIndex; // index into HalfEdges
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FVertexData
    {
        private readonly T FirstHalfEdgeIndex;
    }

    public class FVertexPlanes
    {
        private readonly T NumPlaneIndices;
        private readonly T PlaneIndices; // size 3

        public FVertexPlanes(FArchive Ar)
        {
            PlaneIndices = Ar.Read<T>();
            NumPlaneIndices = Ar.Read<T>();
        }
    }
}
