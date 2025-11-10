using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Sound.Node;

[StructFallback]
public struct FDialogueContext
{
    public FPackageIndex Speaker;
    public FPackageIndex[] Targets;
    
    public FDialogueContext(FStructFallback fallback)
    {
        Speaker = fallback.GetOrDefault<FPackageIndex>(nameof(Speaker));
        Targets = fallback.GetOrDefault<FPackageIndex[]>(nameof(Targets), []);
    }
}