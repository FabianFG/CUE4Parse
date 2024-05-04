using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

[StructFallback]
public class FConstraintInstance
{
    public FName JointName;
    public FName ConstraintBone1;
    public FName ConstraintBone2;

    public FVector Pos1;
    public FVector PriAxis1;
    public FVector SecAxis1;
    
    public FVector Pos2;
    public FVector PriAxis2;
    public FVector SecAxis2;

    public FRotator AngularRotationOffset;
    public int bScaleLinearLimits;

    public FConstraintProfileProperties ProfileInstance;
    
    public FConstraintInstance(FStructFallback fallback)
    {
        JointName = fallback.GetOrDefault<FName>(nameof(JointName));
        ConstraintBone1 = fallback.GetOrDefault<FName>(nameof(ConstraintBone1));
        ConstraintBone2 = fallback.GetOrDefault<FName>(nameof(ConstraintBone2));

        Pos1 = fallback.GetOrDefault<FVector>(nameof(Pos1));
        PriAxis1 = fallback.GetOrDefault<FVector>(nameof(PriAxis1));
        SecAxis1 = fallback.GetOrDefault<FVector>(nameof(SecAxis1));
        
        Pos2 = fallback.GetOrDefault<FVector>(nameof(Pos2));
        PriAxis2 = fallback.GetOrDefault<FVector>(nameof(PriAxis2));
        SecAxis2 = fallback.GetOrDefault<FVector>(nameof(SecAxis2));

        AngularRotationOffset = fallback.GetOrDefault<FRotator>(nameof(AngularRotationOffset));
        bScaleLinearLimits = fallback.GetOrDefault(nameof(bScaleLinearLimits), 1);

        ProfileInstance = fallback.GetOrDefault<FConstraintProfileProperties>(nameof(ProfileInstance));
    }
}

[StructFallback]
public class FConstraintProfileProperties
{
	public float ProjectionLinearTolerance;
	public float ProjectionAngularTolerance;
	public float ProjectionLinearAlpha;
	public float ProjectionAngularAlpha;
	public float ShockPropagationAlpha;
	public float LinearBreakThreshold;
	public float LinearPlasticityThreshold;
	public float AngularBreakThreshold;
	public float AngularPlasticityThreshold;
	public float ContactTransferScale;

	public FLinearConstraint LinearLimit;
	public FConeConstraint ConeLimit;
	public FTwistConstraint TwistLimit;

	public bool bDisableCollision;
	public bool bParentDominates;
	public bool bEnableShockPropagation;
	public bool bEnableProjection;
	public bool bEnableMassConditioning;
	public bool bAngularBreakable;
	public bool bAngularPlasticity;
	public bool bLinearBreakable;
	public bool bLinearPlasticity;

	public FLinearDriveConstraint LinearDrive;
	public FAngularDriveConstraint AngularDrive;
	
	public EConstraintPlasticityType LinearPlasticityType;
    
    public FConstraintProfileProperties(FStructFallback fallback)
    {
	    ProjectionLinearTolerance = fallback.GetOrDefault<float>(nameof(ProjectionLinearTolerance));
	    ProjectionAngularTolerance = fallback.GetOrDefault<float>(nameof(ProjectionAngularTolerance));
	    ProjectionLinearAlpha = fallback.GetOrDefault<float>(nameof(ProjectionLinearAlpha));
	    ProjectionAngularAlpha = fallback.GetOrDefault<float>(nameof(ProjectionAngularAlpha));
	    ShockPropagationAlpha = fallback.GetOrDefault<float>(nameof(ShockPropagationAlpha));
	    LinearBreakThreshold = fallback.GetOrDefault<float>(nameof(LinearBreakThreshold));
	    LinearPlasticityThreshold = fallback.GetOrDefault<float>(nameof(LinearPlasticityThreshold));
	    AngularBreakThreshold = fallback.GetOrDefault<float>(nameof(AngularBreakThreshold));
	    AngularPlasticityThreshold = fallback.GetOrDefault<float>(nameof(AngularPlasticityThreshold));
	    ContactTransferScale = fallback.GetOrDefault<float>(nameof(ContactTransferScale));

	    LinearLimit = fallback.GetOrDefault<FLinearConstraint>(nameof(LinearLimit));
	    ConeLimit = fallback.GetOrDefault<FConeConstraint>(nameof(ConeLimit));
	    TwistLimit = fallback.GetOrDefault<FTwistConstraint>(nameof(TwistLimit));
	    
	    bDisableCollision = fallback.GetOrDefault<bool>(nameof(bDisableCollision));
	    bParentDominates = fallback.GetOrDefault<bool>(nameof(bParentDominates));
	    bEnableShockPropagation = fallback.GetOrDefault<bool>(nameof(bEnableShockPropagation));
	    bEnableProjection = fallback.GetOrDefault<bool>(nameof(bEnableProjection));
	    bEnableMassConditioning = fallback.GetOrDefault<bool>(nameof(bEnableMassConditioning));
	    bAngularBreakable = fallback.GetOrDefault<bool>(nameof(bAngularBreakable));
	    bAngularPlasticity = fallback.GetOrDefault<bool>(nameof(bAngularPlasticity));
	    bLinearBreakable = fallback.GetOrDefault<bool>(nameof(bLinearBreakable));
	    bLinearPlasticity = fallback.GetOrDefault<bool>(nameof(bLinearPlasticity));
	    
	    LinearDrive = fallback.GetOrDefault<FLinearDriveConstraint>(nameof(LinearDrive));
	    AngularDrive = fallback.GetOrDefault<FAngularDriveConstraint>(nameof(AngularDrive));

	    LinearPlasticityType = fallback.GetOrDefault<EConstraintPlasticityType>(nameof(LinearPlasticityType));
    }
}

public enum EConstraintPlasticityType
{
	CCPT_Free,
	CCPT_Shrink,
	CCPT_Grow,
	CCPT_MAX,
}