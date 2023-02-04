using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

[StructFallback]
public class FAnimLinkableElement
{
    public FPackageIndex LinkedMontage;
    public int SlotIndex;
    public int SegmentIndex;
    public EAnimLinkMethod LinkMethod;
    public EAnimLinkMethod CachedLinkMethod;
    public float SegmentBeginTime;
    public float SegmentLength;
    public float LinkValue;
    public FPackageIndex LinkedSequence;

    public FAnimLinkableElement(FStructFallback fallback)
    {
        LinkedMontage = fallback.GetOrDefault<FPackageIndex>(nameof(LinkedMontage));
        SlotIndex = fallback.GetOrDefault<int>(nameof(SlotIndex));
        SegmentIndex = fallback.GetOrDefault<int>(nameof(SegmentIndex));
        LinkMethod = fallback.GetOrDefault<EAnimLinkMethod>(nameof(LinkMethod));
        CachedLinkMethod = fallback.GetOrDefault<EAnimLinkMethod>(nameof(CachedLinkMethod));
        SegmentBeginTime = fallback.GetOrDefault<float>(nameof(SegmentBeginTime));
        SegmentLength = fallback.GetOrDefault<float>(nameof(SegmentLength));
        LinkValue = fallback.GetOrDefault<float>(nameof(LinkValue));
        LinkedSequence = fallback.GetOrDefault<FPackageIndex>(nameof(LinkedSequence));
    }
}

public enum EAnimLinkMethod
{
    Absolute,
    Relative,
    Proportional
}