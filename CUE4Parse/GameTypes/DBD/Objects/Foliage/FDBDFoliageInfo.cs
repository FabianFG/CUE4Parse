using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.Foliage;

[StructFallback]
public readonly struct FDBDFoliageInfo(FStructFallback fallback)
{
    public readonly FPackageIndex? FoliageType = fallback.GetOrDefault<FPackageIndex?>(nameof(FoliageType));
    public readonly FDBDFoliageInstance[] Instances = fallback.GetOrDefault<FDBDFoliageInstance[]>(nameof(Instances), []);
}
