using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[StructFallback]
public readonly struct FWwiseGroupValueCookedData
{
    public readonly EWwiseGroupType Type;
    public readonly int GroupId;
    public readonly int ID;
    public readonly FName DebugName;

    public FWwiseGroupValueCookedData(FStructFallback fallback)
    {
        Type = fallback.GetOrDefault<EWwiseGroupType>(nameof(Type));
        GroupId = fallback.GetOrDefault<int>(nameof(GroupId));
        ID = fallback.GetOrDefault<int>(nameof(ID));
        DebugName = fallback.GetOrDefault<FName>(nameof(DebugName));
    }
}
