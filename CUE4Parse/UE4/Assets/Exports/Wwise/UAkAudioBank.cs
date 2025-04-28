using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkAudioBank : UAkAudioType
{
    public FWwiseLocalizedSoundBankCookedData? SoundBankCookedData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Position >= validPos)
            return;

        SoundBankCookedData = new FWwiseLocalizedSoundBankCookedData(new FStructFallback(Ar, "WwiseLocalizedSoundBankCookedData"));
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (SoundBankCookedData is null)
            return;

        writer.WritePropertyName("SoundBankCookedData");
        serializer.Serialize(writer, SoundBankCookedData);
    }
}
