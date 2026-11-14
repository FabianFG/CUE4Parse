using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[StructFallback]
[JsonConverter(typeof(FWwiseLocalizedShareSetCookedDataConverter))]
public readonly struct FWwiseLocalizedShareSetCookedData
{
    public readonly Dictionary<FWwiseLanguageCookedData, FWwiseShareSetCookedData?> ShareSetLanguageMap;
    public readonly FName DebugName;
    public readonly int ShareSetId;

    public FWwiseLocalizedShareSetCookedData(FStructFallback fallback)
    {
        ShareSetLanguageMap = fallback.GetOrDefault<Dictionary<FWwiseLanguageCookedData, FWwiseShareSetCookedData?>>(nameof(ShareSetLanguageMap), []);
        DebugName = fallback.GetOrDefault<FName>(nameof(DebugName));
        ShareSetId = fallback.GetOrDefault<int>(nameof(ShareSetId));
    }

    public void SerializeBulkData(FAssetArchive Ar)
    {
        foreach (var lang in ShareSetLanguageMap.Values)
        {
            lang?.SerializeBulkData(Ar);
        }
    }
}
