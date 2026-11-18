using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.UE4.Assets.Exports.SkeletalMesh;

[StructFallback]
public struct FSkeletalMeshLODGroupSettings : IUStruct
{
    public readonly FPerPlatformFloat ScreenSize;
    // ... other properties

    public FSkeletalMeshLODGroupSettings(FStructFallback fallback)
    {
        var tag = fallback.Properties.Find(property => property.Name.Text == nameof(ScreenSize))?.Tag;
        ScreenSize = tag is FloatProperty legacyScreenSize ? new FPerPlatformFloat(legacyScreenSize.Value) : fallback.GetOrDefault<FPerPlatformFloat?>(nameof(ScreenSize)) ?? new FPerPlatformFloat(fallback.GetOrDefault<float>(nameof(ScreenSize)));
    }
}
