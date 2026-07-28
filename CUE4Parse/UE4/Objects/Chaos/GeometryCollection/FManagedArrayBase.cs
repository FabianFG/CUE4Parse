using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public abstract class FManagedArrayBase
{
    [JsonIgnore]
    public abstract Array? Data { get; internal set; }

    public abstract void Serialize(FArchive Ar, bool alwaysBulkSerialized);
}
