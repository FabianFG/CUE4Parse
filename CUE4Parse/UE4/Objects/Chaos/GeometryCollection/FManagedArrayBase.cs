namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public abstract class FManagedArrayBase
{
    public abstract void Serialize(FChaosArchive Ar);
    public abstract T[] GetArray<T>();
}