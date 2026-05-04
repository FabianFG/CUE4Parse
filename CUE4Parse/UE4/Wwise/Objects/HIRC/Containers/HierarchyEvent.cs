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
    public HierarchyEvent(FWwiseArchive Ar) : base(Ar)
    {
        if (Ar.Version > 154)
        {
            LimitScope = Ar.Read<byte>();
            InstanceLimit = Ar.Read<ushort>();
            CooldownTime = Ar.Read<float>();
        }

        int eventActionCount;
        if (Ar.Version <= 122)
        {
            eventActionCount = (int) Ar.Read<uint>();
        }
        else
        {
            eventActionCount = Ar.Read7BitEncodedIntBE();
        }
        EventActionIds = Ar.ReadArray<uint>(eventActionCount);
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        if (WwiseConverter.WwiseVersion.Value > 154)
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
