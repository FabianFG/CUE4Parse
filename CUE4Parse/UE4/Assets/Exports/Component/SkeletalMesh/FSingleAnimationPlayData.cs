using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;

[StructFallback]
public readonly struct FSingleAnimationPlayData : IUStruct
{
    public readonly FPackageIndex AnimToPlay;
    public readonly float SavedPosition;
    public readonly float SavedPlayRate;

    public FSingleAnimationPlayData(FStructFallback fallback)
    {
        AnimToPlay = fallback.GetOrDefault(nameof(AnimToPlay), new FPackageIndex());
        SavedPosition = fallback.GetOrDefault(nameof(SavedPosition), 0f);
        SavedPlayRate = fallback.GetOrDefault(nameof(SavedPlayRate), 1f);
    }
}
