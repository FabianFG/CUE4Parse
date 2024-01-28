using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    public class HierarchyEvent : AbstractHierarchy
    {
        public readonly byte EventActionCount;
        public readonly uint[] EventActionIds;

        public HierarchyEvent(FArchive Ar) : base(Ar)
        {
            EventActionCount = Ar.Read<byte>();
            EventActionIds = Ar.ReadArray<uint>(EventActionCount);
        }

        public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            // writer.WritePropertyName("EventActionCount");
            // writer.WriteValue(EventActionCount);

            writer.WritePropertyName("EventActionIds");
            serializer.Serialize(writer, EventActionIds);

            writer.WriteEndObject();
        }
    }
}
