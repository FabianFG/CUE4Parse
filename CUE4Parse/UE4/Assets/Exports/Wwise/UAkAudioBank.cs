using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkAudioBank : UAkAudioType
{
    public FWwiseLocalizedSoundBankCookedData? SoundBankCookedData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Position >= validPos - 4) return;
        switch (Ar.Game)
        {
            case EGame.GAME_HogwartsLegacy or EGame.GAME_Farlight84 or EGame.GAME_ArenaBreakoutInfinite or EGame.GAME_LittleNightmares3:
                return;
            case EGame.GAME_FateTrigger:
            {
                var idk = new FStructFallback(Ar, "AkAudioBank", new FRawHeader([(4, 1)], ERawHeaderFlags.RawProperties));
                Properties.AddRange(idk.Properties);
                return;
            }
            default:
                SoundBankCookedData = new FWwiseLocalizedSoundBankCookedData(new FStructFallback(Ar, "WwiseLocalizedSoundBankCookedData"));
                break;
        }
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
