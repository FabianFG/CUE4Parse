using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Sound.Node;

[StructFallback]
public struct FDialogueContextMapping
{
    public FDialogueContext Context;
    public FPackageIndex SoundWave;

    public FDialogueContextMapping(FStructFallback fallback)
    {
        Context = fallback.GetOrDefault<FDialogueContext>(nameof(Context));
        SoundWave = fallback.GetOrDefault<FPackageIndex>(nameof(SoundWave));
    }
}