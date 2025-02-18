using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkAudioEvent : UAkAudioType
{
    public FStructFallback EventCookedData { get; private set; }
    public float MaximumDuration { get; private set; }
    public float MinimumDuration { get; private set; }
    public bool IsInfinite { get; private set; }
    public float MaxAttenuationRadius { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        EventCookedData = new FStructFallback(Ar, "WwiseLocalizedEventCookedData");
        MaximumDuration = Ar.Read<float>();
        MinimumDuration = Ar.Read<float>();
        IsInfinite = Ar.ReadBoolean();
        MaxAttenuationRadius = Ar.Read<float>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("EventCookedData");
        serializer.Serialize(writer, EventCookedData);

        writer.WritePropertyName("MaximumDuration");
        writer.WriteValue(MaximumDuration);

        writer.WritePropertyName("MinimumDuration");
        writer.WriteValue(MinimumDuration);

        writer.WritePropertyName("IsInfinite");
        writer.WriteValue(IsInfinite);

        writer.WritePropertyName("MaxAttenuationRadius");
        writer.WriteValue(MaxAttenuationRadius);
    }
}
