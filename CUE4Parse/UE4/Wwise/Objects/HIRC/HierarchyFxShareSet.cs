using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchyFxShareSet : BaseHierarchyFx
{
    public HierarchyFxShareSet(FArchive Ar) : base(Ar) { }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WriteEndObject();
    }
}
