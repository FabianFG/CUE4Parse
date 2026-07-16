using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[StructFallback]
public readonly struct FWwiseAuxBusCookedData
{
    public readonly int AuxBusId;
    public readonly FWwiseSoundBankCookedData[] SoundBanks;
    public readonly FWwiseMediaCookedData[] Media;
    public readonly FName DebugName;

    public FWwiseAuxBusCookedData(FStructFallback fallback)
    {
        AuxBusId = fallback.GetOrDefault<int>(nameof(AuxBusId));
        SoundBanks = fallback.GetOrDefault<FWwiseSoundBankCookedData[]>(nameof(SoundBanks), []);
        Media = fallback.GetOrDefault<FWwiseMediaCookedData[]>(nameof(Media), []);
        DebugName = fallback.GetOrDefault<FName>(nameof(DebugName));
    }

    public void SerializeBulkData(FAssetArchive Ar)
    {
        foreach (var sb in SoundBanks)
            sb.SerializeBulkData(Ar);

        foreach (var media in Media)
            media.SerializeBulkData(Ar);
    }
}
