using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Sound.Node;

[StructFallback]
public struct FDialogueWaveParameter
{
    public FPackageIndex DialogueWave;
    public FDialogueContext Context;
    
    public FDialogueWaveParameter(FStructFallback fallback)
    {
        DialogueWave = fallback.GetOrDefault<FPackageIndex>(nameof(DialogueWave));
        Context = fallback.GetOrDefault<FDialogueContext>(nameof(Context));
    }
}