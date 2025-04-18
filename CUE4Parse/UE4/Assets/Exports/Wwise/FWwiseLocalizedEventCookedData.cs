using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[StructFallback]
[JsonConverter(typeof(FWwiseLocalizedEventCookedDataConverter))]
public readonly struct FWwiseLocalizedEventCookedData
{
    public readonly Dictionary<FWwiseLanguageCookedData, FWwiseEventCookedData?> EventLanguageMap;
    public readonly FName DebugName;
    public readonly int EventId;

    public FWwiseLocalizedEventCookedData(FStructFallback fallback)
    {
        EventLanguageMap = new Dictionary<FWwiseLanguageCookedData, FWwiseEventCookedData?>();
        foreach (var kv in fallback.GetOrDefault<UScriptMap>(nameof(EventLanguageMap)).Properties)
        {
            EventLanguageMap[kv.Key.GetValue<FWwiseLanguageCookedData>()] = kv.Value?.GetValue<FWwiseEventCookedData>();
        }

        DebugName = fallback.GetOrDefault<FName>(nameof(DebugName));
        EventId = fallback.GetOrDefault<int>(nameof(EventId));
    }
}
