using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkAudioDeviceShareSet : UAkAudioType
{
    public FStructFallback AudioDeviceShareSetCookedData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        AudioDeviceShareSetCookedData = new FStructFallback(Ar, "WwiseAudioDeviceShareSetCookedData");
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("AudioDeviceShareSetCookedData");
        serializer.Serialize(writer, AudioDeviceShareSetCookedData);
    }
}
