using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkAudioNode : UAkAudioType
{
    public FWwiseAudioNodeCookedData? AudioNodeCookedData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Position >= validPos) return;

        AudioNodeCookedData = new FWwiseAudioNodeCookedData(new FStructFallback(Ar, "WwiseAudioNodeCookedData"));
        AudioNodeCookedData?.SerializeBulkData(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (AudioNodeCookedData is null) return;

        writer.WritePropertyName(nameof(AudioNodeCookedData));
        serializer.Serialize(writer, AudioNodeCookedData);
    }
}
