using CUE4Parse.UE4;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.UE4.Assets.Exports.Rig;

public class FRigLogic
{
    public FRigLogicConfiguration Configuration;
}

[StructFallback]
public class FRigLogicConfiguration : IUStruct
{
    public FPerPlatformERigLogicCalculationType CalculationTypePerPlatform;
    public FPerPlatformERigLogicFloatingPointType FloatingPointTypePerPlatform;
    public FPerPlatformBool EnableMultiThreadMLComputePerPlatform;
    public bool LoadJoints = true;
    public bool LoadBlendShapes = true;
    public bool LoadAnimatedMaps = true;
    public bool LoadMachineLearnedBehavior = true;
    public bool LoadRBFBehavior = true;
    public bool LoadTwistSwingBehavior = true;
    public ERigLogicTranslationType TranslationType = ERigLogicTranslationType.Vector;
    public ERigLogicRotationType RotationType = ERigLogicRotationType.EulerAngles;
    public ERigLogicScaleType ScaleType = ERigLogicScaleType.Vector;
    public float TranslationPruningThreshold;
    public float RotationPruningThreshold;
    public float ScalePruningThreshold;

    public FRigLogicConfiguration(FStructFallback fallback)
    {
        CalculationTypePerPlatform = fallback.GetOrDefault<FPerPlatformERigLogicCalculationType>(nameof(CalculationTypePerPlatform));
        FloatingPointTypePerPlatform = fallback.GetOrDefault<FPerPlatformERigLogicFloatingPointType>(nameof(FloatingPointTypePerPlatform));
        EnableMultiThreadMLComputePerPlatform = fallback.GetOrDefault<FPerPlatformBool>(nameof(EnableMultiThreadMLComputePerPlatform));
        LoadJoints = fallback.GetOrDefault(nameof(LoadJoints), true);
        LoadBlendShapes = fallback.GetOrDefault(nameof(LoadBlendShapes), true);
        LoadAnimatedMaps = fallback.GetOrDefault(nameof(LoadAnimatedMaps), true);
        LoadMachineLearnedBehavior = fallback.GetOrDefault(nameof(LoadMachineLearnedBehavior), true);
        LoadRBFBehavior = fallback.GetOrDefault(nameof(LoadRBFBehavior), true);
        LoadTwistSwingBehavior = fallback.GetOrDefault(nameof(LoadTwistSwingBehavior), true);
        TranslationType = fallback.GetOrDefault(nameof(TranslationType), ERigLogicTranslationType.Vector);
        RotationType = fallback.GetOrDefault(nameof(RotationType), ERigLogicRotationType.EulerAngles);
        ScaleType = fallback.GetOrDefault(nameof(ScaleType), ERigLogicScaleType.Vector);
        TranslationPruningThreshold = fallback.GetOrDefault<float>(nameof(TranslationPruningThreshold));
        RotationPruningThreshold = fallback.GetOrDefault<float>(nameof(RotationPruningThreshold));
        ScalePruningThreshold = fallback.GetOrDefault<float>(nameof(ScalePruningThreshold));
    }
}

public enum ERigLogicTranslationType : uint
{
    None,
    Vector = 3
}

public enum ERigLogicRotationType : uint
{
    None,
    EulerAngles = 3,
    Quaternions = 4
}

public enum ERigLogicScaleType : uint
{
    None,
    Vector = 3
}
