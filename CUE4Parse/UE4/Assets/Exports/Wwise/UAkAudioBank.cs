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
            case GAME_HogwartsLegacy or GAME_Farlight84 or GAME_ArenaBreakoutInfinite or GAME_ArenaBreakoutMobile or GAME_LittleNightmares3:
                return;
            case GAME_FateTrigger:
            {
                var idk = new FStructFallback(Ar, "AkAudioBank", new FRawHeader([(4, 1)], ERawHeaderFlags.RawProperties));
                Properties.AddRange(idk.Properties);
                return;
            }
            case GAME_CenturyAgeofAshes:
            {
                var idk = new FStructFallback(Ar, "AkAudioBank", new FRawHeader([(2, 1)], ERawHeaderFlags.RawProperties));
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

        writer.WritePropertyName(nameof(SoundBankCookedData));
        serializer.Serialize(writer, SoundBankCookedData);
    }
}

public class UWuiBank : UAkAudioBank; // The Awesome Adventures of Captain Spirit
public class UWwiseBank : UAkAudioBank; // Borderlands 3
