using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

[StructFallback]
public readonly struct FSceneLightingDescription
{
    public readonly FName ThemeName;
    public readonly FLightingSpecifics LightingDetailsLowMedium;
    public readonly FLightingSpecifics LightingDetailsHighUltra;

    public FSceneLightingDescription(FStructFallback fallback)
    {
        ThemeName = fallback.GetOrDefault<FName>(nameof(ThemeName));
        LightingDetailsLowMedium = fallback.GetOrDefault<FLightingSpecifics>(nameof(LightingDetailsLowMedium));
        LightingDetailsHighUltra = fallback.GetOrDefault<FLightingSpecifics>(nameof(LightingDetailsHighUltra));
    }
}
