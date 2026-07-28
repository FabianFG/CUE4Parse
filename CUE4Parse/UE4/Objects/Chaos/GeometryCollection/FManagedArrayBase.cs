using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public abstract class FManagedArrayBase
{
    public abstract void Serialize(FArchive Ar, bool alwaysBulkSerialized);
}
