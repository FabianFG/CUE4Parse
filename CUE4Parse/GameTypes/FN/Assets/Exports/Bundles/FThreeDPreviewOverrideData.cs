using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.Bundles;

[StructFallback]
public class FThreeDPreviewOverrideData
{
    public bool bPreviewBoost;
    public bool bPreviewDriftTrail;
    public FLoadoutSlotOverrideData[] LoadoutSlots;

    public FThreeDPreviewOverrideData(FStructFallback fallback)
    {
        bPreviewBoost = fallback.GetOrDefault<bool>(nameof(bPreviewBoost));
        bPreviewDriftTrail = fallback.GetOrDefault<bool>(nameof(bPreviewDriftTrail));
        LoadoutSlots = fallback.GetOrDefault<FLoadoutSlotOverrideData[]>(nameof(LoadoutSlots), []);
    }
}
