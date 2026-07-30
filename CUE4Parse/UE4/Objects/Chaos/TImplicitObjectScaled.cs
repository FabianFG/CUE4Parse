using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Chaos;

public class FImplicitObjectScaled: FImplicitObject
{
    public FVector MScale;
    public FVector MInvScale;
    public float OuterMargin;	//Allows us to inflate the instance before the scale is applied. This is useful when sweeps need to apply a non scale on a geometry with uniform thickness
    public TAABB<float> MLocalBoundingBox;
}
public class TImplicitObjectScaled<T> : FImplicitObjectScaled where T : FImplicitObject
{
    public T? MObject;

    public override void Serialize(FChaosArchive Ar)
    {
        base.Serialize(Ar);
        MObject = Ar.ReadPtr<T>();
        MScale = Ar.Read<FVector>();
        MInvScale = Ar.Read<FVector>();
        MLocalBoundingBox = TBox<float>.SerializeAsAABB(Ar, 3);
    }
}
