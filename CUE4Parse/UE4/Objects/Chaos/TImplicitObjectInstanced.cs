namespace CUE4Parse.UE4.Objects.Chaos;

public class FImplicitObjectInstanced : FImplicitObject;

public class TImplicitObjectInstanced<T> : FImplicitObjectInstanced where T : IChaosClass
{
    public T? MObject;

    public override void Serialize(FChaosArchive Ar)
    {
        base.Serialize(Ar);
        MObject = Ar.ReadPtr<T>();
    }
}