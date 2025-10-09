using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Assets.Exports.Sound;

[StructFallback]
public class FSoundAttenuationSettings : FBaseAttenuationSettings
{
    public FSoundAttenuationSettings(FStructFallback fallback) : base(fallback)
    {
        
    }
}