using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

[StructFallback]
public readonly struct FActorVariationElements(FStructFallback fallback)
{
    public readonly ETileVariation Type = fallback.GetOrDefault<ETileVariation>(nameof(Type));
    public readonly FSoftObjectPath Element = fallback.GetOrDefault<FSoftObjectPath>(nameof(Element));
}
