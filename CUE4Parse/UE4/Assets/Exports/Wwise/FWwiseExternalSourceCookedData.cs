using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[StructFallback]
public readonly struct FWwiseExternalSourceCookedData
{
    public readonly int Cookie;
    public readonly FName DebugName;

    public FWwiseExternalSourceCookedData(FStructFallback fallback)
    {
        Cookie = fallback.GetOrDefault<int>(nameof(Cookie));
        DebugName = fallback.GetOrDefault<FName>(nameof(DebugName));
    }
}
