using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.Sound;

[StructFallback]
public class FSoundSequenceData
{
    public USoundCue Sound;
    public float Delay;

    public FSoundSequenceData(FStructFallback fallback)
    {
        Sound = fallback.GetOrDefault<USoundCue>(nameof(Sound));
        Delay = fallback.GetOrDefault<float>(nameof(Delay));
    }
}
