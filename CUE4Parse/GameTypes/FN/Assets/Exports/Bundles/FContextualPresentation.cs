using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.Bundles;

[StructFallback]
public class FContextualPresentation
{
    public FGameplayTag ProductTag;
    public FSoftObjectPath RenderImage;
    public FSoftObjectPath OverrideImageMaterial;

    public FContextualPresentation(FStructFallback fallback)
    {
        ProductTag = fallback.GetOrDefault<FGameplayTag>(nameof(ProductTag));
        RenderImage = fallback.GetOrDefault<FSoftObjectPath>(nameof(RenderImage));
        OverrideImageMaterial = fallback.GetOrDefault<FSoftObjectPath>(nameof(OverrideImageMaterial));
    }
}
