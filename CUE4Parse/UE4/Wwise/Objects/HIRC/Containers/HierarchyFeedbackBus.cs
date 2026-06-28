using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

// Legacy HIRC <= 125
public class HierarchyFeedbackBus(FWwiseArchive Ar) : BaseHierarchyBus(Ar)
{
    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WriteEndObject();
    }
}
