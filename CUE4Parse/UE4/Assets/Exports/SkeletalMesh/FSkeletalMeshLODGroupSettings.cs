using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

[StructFallback]
public struct FSkeletalMeshLODGroupSettings : IUStruct
{
    public readonly TPerPlatformProperty.FPerPlatformFloat ScreenSize;
    // ... other properties

    public FSkeletalMeshLODGroupSettings(FStructFallback fallback)
    {
        ScreenSize = fallback.GetOrDefault<TPerPlatformProperty.FPerPlatformFloat>(nameof(ScreenSize));
        if (ScreenSize is null)
        {
            ScreenSize = new TPerPlatformProperty.FPerPlatformFloat(fallback.GetOrDefault<float>(nameof(ScreenSize)));
        }
    }
}
