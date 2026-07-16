using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

// CAkIndexable
public abstract class AbstractHierarchy : ICAkIndexable
{
    public uint Id { get; set; }
    public abstract void WriteJson(JsonWriter writer, JsonSerializer serializer);
}
