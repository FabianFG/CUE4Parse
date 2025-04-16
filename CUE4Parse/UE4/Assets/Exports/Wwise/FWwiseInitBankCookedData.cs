using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class FWwiseInitBankCookedData
{
    public List<FWwiseSoundBankCookedData> SoundBanks { get; private set; } = [];
    public List<FWwiseMediaCookedData> Media { get; private set; } = [];
    public List<FWwiseLanguageCookedData> Language { get; private set; } = [];

    public FWwiseInitBankCookedData() { }

    public FWwiseInitBankCookedData(FStructFallback fallback)
    {
        SoundBanks = fallback.GetOrDefault("SoundBanks", new List<FWwiseSoundBankCookedData>());
        Media = fallback.GetOrDefault("Media", new List<FWwiseMediaCookedData>());
        Language = fallback.GetOrDefault("Language", new List<FWwiseLanguageCookedData>());
    }
}
