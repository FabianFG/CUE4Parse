using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.Sound;

[StructFallback]
public class FBaseAttenuationSettings
{
    public readonly float FalloffDistance;
    
    public FBaseAttenuationSettings(FStructFallback fallback)
    {
        FalloffDistance = fallback.GetOrDefault<float>(nameof(FalloffDistance));
    }
}