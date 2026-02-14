using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[StructFallback]
[JsonConverter(typeof(FWwiseLocalizedAuxBusCookedDataConverter))]
public readonly struct FWwiseLocalizedAuxBusCookedData
{
    public readonly Dictionary<FWwiseLanguageCookedData, FWwiseAuxBusCookedData?> AuxBusLanguageMap;
    public readonly FName DebugName;
    public readonly int AuxBusId;

    public FWwiseLocalizedAuxBusCookedData(FStructFallback fallback)
    {
        AuxBusLanguageMap = new Dictionary<FWwiseLanguageCookedData, FWwiseAuxBusCookedData?>();
        foreach (var kv in fallback.GetOrDefault<UScriptMap>(nameof(AuxBusLanguageMap)).Properties)
        {
            AuxBusLanguageMap[kv.Key.GetValue<FWwiseLanguageCookedData>()] = kv.Value?.GetValue<FWwiseAuxBusCookedData>();
        }

        DebugName = fallback.GetOrDefault<FName>(nameof(DebugName));
        AuxBusId = fallback.GetOrDefault<int>(nameof(AuxBusId));
    }

    public void SerializeBulkData(FAssetArchive Ar)
    {
        foreach (var lang in AuxBusLanguageMap.Values)
        {
            lang?.SerializeBulkData(Ar);
        }
    }
}
