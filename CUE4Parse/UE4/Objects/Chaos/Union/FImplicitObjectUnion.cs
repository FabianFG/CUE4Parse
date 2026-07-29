using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Chaos.Union;

public class FImplicitObjectUnion : FImplicitObject
{
    public FImplicitObject?[]? MObjects { get; set; }
    public TAABB<float> MLocalBoundingBox { get; set; }
    public int NumLeafObjects;

    public bool bAllowBVH;
    public bool bHasBVH;
    public bool bIsLocked;

    public override void Serialize(FChaosArchive Ar)
    {
        base.Serialize(Ar);

        MObjects = Ar.ReadPtrArray<FImplicitObject>();
        MLocalBoundingBox = TBox<float>.SerializeAsAABB(Ar, 3);
        
        if (FExternalPhysicsCustomObjectVersion.Get(Ar) < FExternalPhysicsCustomObjectVersion.Type.UnionObjectsCanAvoidHierarchy)
        {
            throw new NotImplementedException();
        }
        else if (FFortniteMainBranchObjectVersion.Get(Ar) < FFortniteMainBranchObjectVersion.Type.ChaosImplicitObjectUnionBVHRefactor)
        {
            throw new NotImplementedException();
        }
        else
        {
            var bits = Ar.Read<byte>();

            bAllowBVH = (bits & 1) != 0;
            bHasBVH = (bits & 2) != 0;
            bIsLocked = (bits & 4) != 0;

            NumLeafObjects = FFortniteSeasonBranchObjectVersion.Get(Ar) < FFortniteSeasonBranchObjectVersion.Type.ChaosImplicitObjectUnionLeafObjectsToInt32 ? Ar.Read<ushort>() : Ar.Read<int>();

            if (bHasBVH) throw new NotImplementedException();
        }
    }
}
