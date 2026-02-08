using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[StructFallback]
public class FWwiseInitBankCookedData : FWwiseSoundBankCookedData
{
    public FWwiseSoundBankCookedData[] SoundBanks;
    public FWwiseMediaCookedData[] Media;
    public FWwiseLanguageCookedData[] Language;

    public FWwiseInitBankCookedData(FStructFallback fallback) : base(fallback)
    {
        SoundBanks = fallback.GetOrDefault<FWwiseSoundBankCookedData[]>(nameof(SoundBanks), []);
        Media = fallback.GetOrDefault<FWwiseMediaCookedData[]>(nameof(Media), []);
        Language = fallback.GetOrDefault<FWwiseLanguageCookedData[]>(nameof(Language), []);
    }

    public override void SerializeBulkData(FAssetArchive Ar)
    {
        base.SerializeBulkData(Ar);

        foreach (var sb in SoundBanks)
            sb.SerializeBulkData(Ar);

        foreach (var media in Media)
            media.SerializeBulkData(Ar);
    }
}
