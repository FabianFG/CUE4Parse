namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public class TManagedArray<T> : TManagedArrayBase<T>
{
    public TManagedArray(Func<T> func, bool bReadAsNormalArray = true) : base(func, bReadAsNormalArray) { }
}