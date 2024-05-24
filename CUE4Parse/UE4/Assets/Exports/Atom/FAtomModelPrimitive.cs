using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Atom;

[StructFallback]
public readonly struct FAtomModelPrimitive
{
    public readonly FAtomModelPart[] Parts;
    public readonly int DesignId;
    public readonly FGuid UUID;
    public readonly FName DesignName;

    public FAtomModelPrimitive(FStructFallback fallback)
    {
        Parts = fallback.GetOrDefault<FAtomModelPart[]>(nameof(Parts), []);
        DesignId = fallback.GetOrDefault<int>(nameof(DesignId));
        UUID = fallback.GetOrDefault<FGuid>(nameof(UUID));
        DesignName = fallback.GetOrDefault<FName>(nameof(DesignName));
    }
}
