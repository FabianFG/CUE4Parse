using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.HairStrands;

[StructFallback]
public sealed class FHairGroupsRendering(FStructFallback fallback) : IUStruct
{
    public readonly FHairGeometrySettings? GeometrySettings = fallback.GetOrDefault<FHairGeometrySettings>(nameof(GeometrySettings));
    public readonly FHairShadowSettings? ShadowSettings = fallback.GetOrDefault<FHairShadowSettings>(nameof(ShadowSettings));
    public readonly FName MaterialSlotName = fallback.GetOrDefault<FName>(nameof(MaterialSlotName));
    public readonly FPackageIndex? Material = fallback.GetOrDefault<FPackageIndex>(nameof(Material));
}

[StructFallback]
public sealed class FHairShadowSettings(FStructFallback fallback) : IUStruct
{
    public readonly float HairShadowDensity = fallback.GetOrDefault(nameof(HairShadowDensity), 1f);
    public readonly bool bVoxelize = fallback.GetOrDefault(nameof(bVoxelize), true);
}

[StructFallback]
public sealed class FHairGeometrySettings(FStructFallback fallback) : IUStruct
{
    public static readonly FHairGeometrySettings Default = new(new FStructFallback());

    public readonly bool HairWidth_Override = fallback.GetOrDefault<bool>(nameof(HairWidth_Override));
    public readonly float HairWidth = fallback.GetOrDefault(nameof(HairWidth), 0.01f);
    public readonly float HairRootScale = fallback.GetOrDefault(nameof(HairRootScale), 1f);
    public readonly float HairTipScale = fallback.GetOrDefault(nameof(HairTipScale), 1f);
}
