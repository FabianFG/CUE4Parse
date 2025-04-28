using System.Collections.Generic;
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
    public int SoundBankId { get; set; }
    public List<FName> IncludedEventNames { get; set; } = [];

    public FWwiseLocalizedSoundBankCookedData(FStructFallback fallback)
    {
        SoundBankLanguageMap = new Dictionary<FWwiseLanguageCookedData, FWwiseSoundBankCookedData?>();
        foreach (var kv in fallback.GetOrDefault<UScriptMap>(nameof(SoundBankLanguageMap)).Properties)
        {
            SoundBankLanguageMap[kv.Key.GetValue<FWwiseLanguageCookedData>()] = kv.Value?.GetValue<FWwiseSoundBankCookedData>();
        }
        DebugName = fallback.GetOrDefault<FName>(nameof(DebugName));
        SoundBankId = fallback.GetOrDefault<int>(nameof(SoundBankId));
        IncludedEventNames = fallback.GetOrDefault<List<FName>>(nameof(IncludedEventNames));
    }
}
