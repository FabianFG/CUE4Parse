using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkInitBank : UAkAudioType
{
    public List<FWwiseSoundBankCookedData> SoundBanks { get; private set; } = new();
    public List<FWwiseMediaCookedData> Media { get; private set; } = new();
    public List<FWwiseLanguageCookedData> Language { get; private set; } = new();

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Position >= validPos)
            return;

        var initBankData = new FWwiseInitBankCookedData(new FStructFallback(Ar, "WwiseInitBankCookedData"));

        SoundBanks = initBankData.SoundBanks;
        Media = initBankData.Media;
        Language = initBankData.Language;
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("SoundBanks");
        serializer.Serialize(writer, SoundBanks);

        writer.WritePropertyName("Media");
        serializer.Serialize(writer, Media);

        writer.WritePropertyName("Language");
        serializer.Serialize(writer, Language);
    }
}
