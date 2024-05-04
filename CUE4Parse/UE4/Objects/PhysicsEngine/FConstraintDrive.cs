using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

[StructFallback]
public class FConstraintDrive
{
    public float Stiffness;
    public float Damping;
    public float MaxForce;
    public bool bEnablePositionDrive;
    public bool bEnableVelocityDrive;
    
    public FConstraintDrive(FStructFallback fallback)
    {
        Stiffness = fallback.GetOrDefault<float>(nameof(Stiffness));
        Damping = fallback.GetOrDefault<float>(nameof(Damping));
        MaxForce = fallback.GetOrDefault<float>(nameof(MaxForce));
        bEnablePositionDrive = fallback.GetOrDefault<bool>(nameof(bEnablePositionDrive));
        bEnableVelocityDrive = fallback.GetOrDefault<bool>(nameof(bEnableVelocityDrive));
    }
}

[StructFallback]
public class FLinearDriveConstraint
{
    public FVector PositionTarget;
    public FVector VelocityTarget;
    public FConstraintDrive XDrive;
    public FConstraintDrive YDrive;
    public FConstraintDrive ZDrive;
    
    public FLinearDriveConstraint(FStructFallback fallback)
    {
        PositionTarget = fallback.GetOrDefault<FVector>(nameof(PositionTarget));
        VelocityTarget = fallback.GetOrDefault<FVector>(nameof(VelocityTarget));
        XDrive = fallback.GetOrDefault<FConstraintDrive>(nameof(XDrive));
        YDrive = fallback.GetOrDefault<FConstraintDrive>(nameof(YDrive));
        ZDrive = fallback.GetOrDefault<FConstraintDrive>(nameof(ZDrive));
    }
}

[StructFallback]
public class FAngularDriveConstraint
{
    public FConstraintDrive TwistDrive;
    public FConstraintDrive SwingDrive;
    public FConstraintDrive SlerpDrive;
    public FRotator OrientationTarget;
    public FVector AngularVelocityTarget;
    public EAngularDriveMode AngularDriveMode;
    
    public FAngularDriveConstraint(FStructFallback fallback)
    {
        TwistDrive = fallback.GetOrDefault<FConstraintDrive>(nameof(TwistDrive));
        SwingDrive = fallback.GetOrDefault<FConstraintDrive>(nameof(SwingDrive));
        SlerpDrive = fallback.GetOrDefault<FConstraintDrive>(nameof(SlerpDrive));
        OrientationTarget = fallback.GetOrDefault<FRotator>(nameof(OrientationTarget));
        AngularVelocityTarget = fallback.GetOrDefault<FVector>(nameof(AngularVelocityTarget));
        AngularDriveMode = fallback.GetOrDefault<EAngularDriveMode>(nameof(AngularDriveMode));
    }
}

public enum EAngularDriveMode
{
    SLERP,
    TwistAndSwing
};