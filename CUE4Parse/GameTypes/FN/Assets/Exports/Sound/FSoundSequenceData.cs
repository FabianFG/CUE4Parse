using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.Sound;

[StructFallback]
public class FSoundSequenceData
{
    public FPackageIndex Sound;
    public float Delay;

    public FSoundSequenceData(FStructFallback fallback)
    {
        Sound = fallback.GetOrDefault<FPackageIndex>(nameof(Sound));
        Delay = fallback.GetOrDefault<float>(nameof(Delay));
    }
}
