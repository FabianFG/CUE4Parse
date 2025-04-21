using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkTrigger : UAkAudioType
{
    public FStructFallback? TriggerCookedData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Position >= validPos) return;

        TriggerCookedData = new FStructFallback(Ar, "WwiseTriggerCookedData");
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (TriggerCookedData is null) return;

        writer.WritePropertyName("TriggerCookedData");
        serializer.Serialize(writer, TriggerCookedData);
    }
}
