using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.Foliage;

[StructFallback]
public readonly struct FDBDFoliageInstance(FStructFallback fallback)
{
    public readonly int BaseID = fallback.GetOrDefault<int>(nameof(BaseID));
    public readonly FVector Location = fallback.GetOrDefault<FVector>(nameof(Location));
    public readonly FRotator Rotation = fallback.GetOrDefault<FRotator>(nameof(Rotation));
    public readonly FVector DrawScale3D = fallback.GetOrDefault(nameof(DrawScale3D), FVector.OneVector);

    public FTransform GetTransform() => new(Rotation, Location, DrawScale3D);
}
