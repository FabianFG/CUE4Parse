using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.Bundles;

[StructFallback]
public class FCustomizationInfoData
{
    public string VariantTagString;
    public string VariantChannelTagString;
    public FLinearColor ColorValue;

    public FCustomizationInfoData(FStructFallback fallback)
    {
        VariantTagString = fallback.GetOrDefault(nameof(VariantTagString), string.Empty);
        VariantChannelTagString = fallback.GetOrDefault(nameof(VariantChannelTagString), string.Empty);
        ColorValue = fallback.GetOrDefault<FLinearColor>(nameof(ColorValue));
    }
}
