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
        AuxBusLanguageMap = fallback.GetOrDefault<Dictionary<FWwiseLanguageCookedData, FWwiseAuxBusCookedData?>>(nameof(AuxBusLanguageMap), []);
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
