using CUE4Parse.UE4.Objects.Chaos.Union;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Chaos;

public class FCapsule : FImplicitObject
{
    public float ArRadius;
    public TSegment<float> MSegment;
    public TAABB<float> DummyBox;
    public FCapsule() { }

    public override void Serialize(FChaosArchive Ar)
    {
        base.Serialize(Ar);
        MSegment = Ar.Read<TSegment<float>>();
        ArRadius = Ar.Read<float>();
        if (FExternalPhysicsCustomObjectVersion.Get(Ar) < FExternalPhysicsCustomObjectVersion.Type.CapsulesNoUnionOrAABBs)
        {
            //no longer store this, computed on demand
            DummyBox = TBox<float>.SerializeAsAABB(Ar, 3);
        }

        if (FExternalPhysicsCustomObjectVersion.Get(Ar) < FExternalPhysicsCustomObjectVersion.Type.CapsulesNoUnionOrAABBs)
        {
            var TmpUnion = Ar.ReadPtr<FImplicitObjectUnion>();
        }
    }
}

public struct TSegment<T>
{
    public TIntVector3<T> MPoint;
    public TIntVector3<T> MAxis;
    public T MLength;

    public TSegment(FArchive Ar)
    {
        MPoint = Ar.Read<TIntVector3<T>>();
        MAxis = Ar.Read<TIntVector3<T>>();
        MLength = Ar.Read<T>();
    }
}
