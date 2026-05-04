using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

public class HierarchySidechainMix : AbstractHierarchy
{
    public readonly AkChannelConfig ChannelConfig;

    // CAkSidechainMixIndexable::SetInitialValues
    public HierarchySidechainMix(FWwiseArchive Ar) : base(Ar)
    {
        ChannelConfig = new AkChannelConfig(Ar);
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(ChannelConfig));
        serializer.Serialize(writer, ChannelConfig);

        writer.WriteEndObject();
    }
}
