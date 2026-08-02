using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Animation;

public class UAnimStreamable : UAnimSequenceBase
{
    public int NumberOfKeys;
    public EAnimInterpolationType Interpolation;
    public FName? RetargetSource;
    public FFrameRate? SamplingFrameRate;
    public int NumFrames;
    public FPackageIndex? BoneCompressionSettings;
    public FPackageIndex? CurveCompressionSettings;
    public FPackageIndex? VariableFrameStrippingSettings;
    public bool bEnableRootMotion;
    public ERootMotionRootLock RootMotionRootLock;
    public bool bForceRootLock;
    public bool bUseNormalizedRootMotionScale;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        NumberOfKeys = GetOrDefault<int>(nameof(NumberOfKeys));
        Interpolation = GetOrDefault<EAnimInterpolationType>(nameof(Interpolation));
        RetargetSource = GetOrDefault<FName?>(nameof(RetargetSource));
        SamplingFrameRate = GetOrDefault<FFrameRate?>(nameof(SamplingFrameRate));
        NumFrames = GetOrDefault<int>(nameof(NumFrames));
        BoneCompressionSettings = GetOrDefault<FPackageIndex?>(nameof(BoneCompressionSettings));
        CurveCompressionSettings = GetOrDefault<FPackageIndex?>(nameof(CurveCompressionSettings));
        VariableFrameStrippingSettings = GetOrDefault<FPackageIndex?>(nameof(VariableFrameStrippingSettings));
        bEnableRootMotion = GetOrDefault<bool>(nameof(bEnableRootMotion));
        RootMotionRootLock = GetOrDefault<ERootMotionRootLock>(nameof(RootMotionRootLock));
        bForceRootLock = GetOrDefault<bool>(nameof(bForceRootLock));
        bUseNormalizedRootMotionScale = GetOrDefault<bool>(nameof(bUseNormalizedRootMotionScale));
    }
}

public enum ERootMotionRootLock : byte
{
    /** Use reference pose root bone position. */
    RefPose,

    /** Use root bone position on first frame of animation. */
    AnimFirstFrame,

    /** FTransform::Identity. */
    Zero
}
