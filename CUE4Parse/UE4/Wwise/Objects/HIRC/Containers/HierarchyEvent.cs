using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

// CAkEvent
public class HierarchyEvent : AbstractHierarchy
{
    public readonly byte LimitScope;
    public readonly ushort InstanceLimit;
    public readonly float CooldownTime;
    public readonly uint[] EventActionIds;

    // CAkEvent::SetInitialValues
    public HierarchyEvent(FArchive Ar) : base(Ar)
    {
        if (WwiseVersions.Version > 154)
        {
            LimitScope = Ar.Read<byte>();
            InstanceLimit = Ar.Read<ushort>();
            CooldownTime = Ar.Read<float>();
        }

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

        if (WwiseVersions.Version > 154)
        {
            writer.WritePropertyName(nameof(LimitScope));
            writer.WriteValue(LimitScope);

            writer.WritePropertyName(nameof(InstanceLimit));
            writer.WriteValue(InstanceLimit);

            writer.WritePropertyName(nameof(CooldownTime));
            writer.WriteValue(CooldownTime);
        }

        writer.WritePropertyName(nameof(EventActionIds));
        serializer.Serialize(writer, EventActionIds);

        writer.WriteEndObject();
    }
}
