using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Chaos;

public sealed class TBox<T> : FImplicitObject where T : struct
{
    public TAABB<T> AABB;
    
    public override void Serialize(FChaosArchive Ar)
    {
        base.Serialize(Ar);
        
        AABB = new TAABB<T>(Ar, 3);
        if (FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.MarginAddedToConvexAndBox)
            Margin = Ar.Read<float>();
    }

    public static TAABB<T> SerializeAsAABB(FChaosArchive Ar, int dimensions) 
    {
        if (FExternalPhysicsCustomObjectVersion.Get(Ar) < FExternalPhysicsCustomObjectVersion.Type.TBoxReplacedWithTAABB)
        {
            var box = new TBox<T>();
            box.Serialize(Ar);
            return box.AABB;
        }
        else
        {
            return new TAABB<T>(Ar, dimensions);
        }
    }

    public static Dictionary<int, TAABB<T>> SerializeAsAABBs(FChaosArchive Ar, int dimensions) => Ar.ReadMap(Ar.Read<int>, () => SerializeAsAABB(Ar, dimensions));
}