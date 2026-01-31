using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

// TODO: Check if this is read correctly, I didn't have any test samples
public class HierarchySidechainMix : AbstractHierarchy
{
    public readonly uint SidechainId;
    public readonly EAkChannelConfigType ChannelConfig;

    // CAkSidechainMixIndexable::SetInitialValues
    public HierarchySidechainMix(FArchive Ar) : base(Ar)
    {
        SidechainId = Ar.Read<uint>();
        ChannelConfig = Ar.Read<EAkChannelConfigType>();
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(SidechainId));
        writer.WriteValue(SidechainId);

        writer.WritePropertyName(nameof(ChannelConfig));
        writer.WriteValue(ChannelConfig.ToString());

        writer.WriteEndObject();
    }
}
