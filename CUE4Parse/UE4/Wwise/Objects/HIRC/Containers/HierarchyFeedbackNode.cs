using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

// Legacy HIRC <= 125
public class HierarchyFeedbackNode(FWwiseArchive Ar) : BaseHierarchyBus(Ar)
{
    // TODO: Won't be read correctly
    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WriteEndObject();
    }
}
