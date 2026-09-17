using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[StructFallback]
public struct FWwiseGroupValueCookedDataSet
{
    public FWwiseGroupValueCookedData[] GroupValues;

    public FWwiseGroupValueCookedDataSet(FStructFallback fallback)
    {
        GroupValues = fallback.GetOrDefault<FWwiseGroupValueCookedData[]>(nameof(GroupValues), []);
    }
}
