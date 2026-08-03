namespace CUE4Parse.UE4.Objects.Chaos;

public class TPlane<T> : FImplicitObject where T: struct
{
    public TCorePlane<T> MPlaneConcrete;
    public T Distance;

    public override void Serialize(FChaosArchive Ar)
    {
        base.Serialize(Ar);
        MPlaneConcrete = new TCorePlane<T>(Ar, 3);
    }
}
