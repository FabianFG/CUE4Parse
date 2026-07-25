using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Chaos;

public class TImplicitObjectTransformed : FImplicitObject
{
    public FImplicitObject? MObject { get; set; }
    public FTransform MTransform { get; set; }
    public TAABB<float> AABB { get; set; }
    
    public override void Serialize(FChaosArchive Ar)
    {
        base.Serialize(Ar);

        MObject = Ar.ReadPtr<FImplicitObject>();
        MTransform = Ar.Read<TTransform<double>>();
        AABB = TBox<float>.SerializeAsAABB(Ar, 3);
    }
}