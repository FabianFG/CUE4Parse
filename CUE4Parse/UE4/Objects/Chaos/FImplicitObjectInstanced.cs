
namespace CUE4Parse.UE4.Assets.Exports.Chaos;

public class FImplicitObjectInstanced: FImplicitObject
{
    protected FReal OuterMargin;
}

public class TImplicitObjectInstanced<T> : FImplicitObjectInstanced where T : FImplicitObject
{
    // TRefCountPtr<TConcrete>;
    protected T MObject;

    public TImplicitObjectInstanced()
    {
        MObject = (T)Activator.CreateInstance(typeof(T));
    }

    public override ISerializationFactory Serialize(FChaosArchive Ar)
    {
        base.Serialize(Ar);
        Ar.SerializePtr(MObject);
        return this;
    }
}
