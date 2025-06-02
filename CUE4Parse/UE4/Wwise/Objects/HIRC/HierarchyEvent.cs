using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchyEvent : AbstractHierarchy
{
    public readonly uint[] EventActionIds;

    public HierarchyEvent(FArchive Ar) : base(Ar)
    {
        int eventActionCount;
        if (WwiseVersions.Version <= 122)
        {
            eventActionCount = (int) Ar.Read<uint>();
        }
        else
        {
            eventActionCount = WwiseReader.Read7BitEncodedIntBE(Ar);
        }
        EventActionIds = Ar.ReadArray<uint>(eventActionCount);
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("EventActionIds");
        serializer.Serialize(writer, EventActionIds);

        writer.WriteEndObject();
    }
}
