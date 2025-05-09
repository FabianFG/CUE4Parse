using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public abstract class AbstractHierarchy(FArchive Ar)
{
    public uint Id { get; } = Ar.Read<uint>();

    public abstract void WriteJson(JsonWriter writer, JsonSerializer serializer);
}
