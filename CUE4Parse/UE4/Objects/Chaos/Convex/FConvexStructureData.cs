using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Chaos.Convex;

public class FConvexStructureData
{
    public EIndexType IndexType;
    public FStructureData Data;
    
    public FConvexStructureData(FChaosArchive Ar)
    {
        var bUseHalfEdgeStructureData = FPhysicsObjectVersion.Get(Ar) >= FPhysicsObjectVersion.Type.ChaosConvexUsesHalfEdges;

        if (!bUseHalfEdgeStructureData)
            throw new NotImplementedException("Loading legacy convex structure data is not implemented");
        
        IndexType = Ar.Read<EIndexType>();
        Data = new FStructureData();

        switch (IndexType)
        {
            case EIndexType.Small: Data.DataS = new TConvexHalfEdgeStructureData<byte>(Ar); break;
            case EIndexType.Medium: Data.DataM = new TConvexHalfEdgeStructureData<short>(Ar); break;
            case EIndexType.Large: Data.DataL = new TConvexHalfEdgeStructureData<int>(Ar); break;
        }
    }
}

public enum EIndexType : sbyte
{
    None,
    Small,
    Medium,
    Large,
}