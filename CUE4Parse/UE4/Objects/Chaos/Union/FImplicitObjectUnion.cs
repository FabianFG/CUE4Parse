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
            // LegacySerializeBVH(Ar);
            // bHierarchyBuilt = Ar.ReadBoolean();
            throw new NotImplementedException("Legacy BVH serialization is not implemented");
        }
        else if (FFortniteMainBranchObjectVersion.Get(Ar) < FFortniteMainBranchObjectVersion.Type.ChaosImplicitObjectUnionBVHRefactor)
        {
            // bHierarchyBuilt = Ar.ReadBoolean();
            // if (bHierarchyBuilt) LegacySerializeBVH(Ar);
            throw new NotImplementedException("Legacy BVH serialization is not implemented");
        }
        else
        {
            var bits = Ar.Read<byte>();

            bAllowBVH = (bits & 1) != 0;
            bHasBVH = (bits & 2) != 0;
            bIsLocked = (bits & 4) != 0;

            NumLeafObjects = FFortniteSeasonBranchObjectVersion.Get(Ar) < FFortniteSeasonBranchObjectVersion.Type.ChaosImplicitObjectUnionLeafObjectsToInt32 ? Ar.Read<ushort>() : Ar.Read<int>();

            if (bHasBVH)
            {
                var bvh = new FImplicitBVH(Ar);
            }
        }
    }
}
