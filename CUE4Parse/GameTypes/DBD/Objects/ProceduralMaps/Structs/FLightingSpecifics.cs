using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

[StructFallback]
public readonly struct FLightingSpecifics
{
    public readonly FSoftObjectPath LightBlueprint;
    public readonly FSoftObjectPath LightAmbiance;

    public FLightingSpecifics(FStructFallback fallback)
    {
        LightBlueprint = fallback.GetOrDefault<FSoftObjectPath>(nameof(LightBlueprint));
        LightAmbiance = fallback.GetOrDefault<FSoftObjectPath>(nameof(LightAmbiance));
    }
}
