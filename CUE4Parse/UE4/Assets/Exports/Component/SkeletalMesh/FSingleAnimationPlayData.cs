using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;

[StructFallback]
public readonly struct FSingleAnimationPlayData : IUStruct
{
    public readonly FPackageIndex AnimToPlay;

    public FSingleAnimationPlayData(FStructFallback fallback)
    {
        AnimToPlay = fallback.GetOrDefault(nameof(AnimToPlay), new FPackageIndex());
    }
}
