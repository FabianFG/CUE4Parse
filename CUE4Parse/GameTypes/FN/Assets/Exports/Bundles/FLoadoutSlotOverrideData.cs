using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.Bundles;

[StructFallback]
public class FLoadoutSlotOverrideData
{
    public string TemplateId;
    public bool bMainSlot;
    public FCustomizationInfoData[] VariantOverrides;
    public FPackageIndex ItemDefinition;

    public FLoadoutSlotOverrideData(FStructFallback fallback)
    {
        TemplateId = fallback.GetOrDefault(nameof(TemplateId), string.Empty);
        bMainSlot = fallback.GetOrDefault<bool>(nameof(bMainSlot));
        VariantOverrides = fallback.GetOrDefault<FCustomizationInfoData[]>(nameof(VariantOverrides), []);
        ItemDefinition = fallback.GetOrDefault<FPackageIndex>(nameof(ItemDefinition));
    }
}
