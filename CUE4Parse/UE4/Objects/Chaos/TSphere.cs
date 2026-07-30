using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.Chaos;

public class TSphere : FImplicitObject
{
    public FVector Center;
    public float Radius;

    public override void Serialize(FChaosArchive Ar)
    {
        base.Serialize(Ar);
        Center = Ar.Read<FVector>();
        Radius = Ar.Read<float>();
    }
}
