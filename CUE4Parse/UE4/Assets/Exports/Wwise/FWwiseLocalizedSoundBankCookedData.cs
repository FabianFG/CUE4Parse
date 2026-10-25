using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[StructFallback]
[JsonConverter(typeof(FWwiseLocalizedSoundBankCookedDataConverter))]
public class FWwiseLocalizedSoundBankCookedData
{
    public Dictionary<FWwiseLanguageCookedData, FWwiseSoundBankCookedData?> SoundBankLanguageMap { get; set; } = [];
    public FName DebugName { get; set; }
    public uint SoundBankId { get; set; }
    public List<FName> IncludedEventNames { get; set; } = [];

    public FWwiseLocalizedSoundBankCookedData(FStructFallback fallback)
    {
        SoundBankLanguageMap = fallback.GetOrDefault<Dictionary<FWwiseLanguageCookedData, FWwiseSoundBankCookedData?>>(nameof(SoundBankLanguageMap), []);
        DebugName = fallback.GetOrDefault<FName>(nameof(DebugName));
        SoundBankId = (uint)fallback.GetOrDefault<int>(nameof(SoundBankId));
        IncludedEventNames = fallback.GetOrDefault<List<FName>>(nameof(IncludedEventNames));
    }
}
