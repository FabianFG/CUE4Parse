using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

[StructFallback]
public class FConstraintBaseParams
{
    public float Stiffness;
    public float Damping;
    public float Restitution;
    public float ContactDistance;
    public bool bSoftConstraint;
    
    public FConstraintBaseParams(FStructFallback fallback)
    {
        Stiffness = fallback.GetOrDefault<float>(nameof(Stiffness));
        Damping = fallback.GetOrDefault<float>(nameof(Damping));
        Restitution = fallback.GetOrDefault<float>(nameof(Restitution));
        ContactDistance = fallback.GetOrDefault<float>(nameof(ContactDistance));
        bSoftConstraint = fallback.GetOrDefault<bool>(nameof(bSoftConstraint));
    }
}

[StructFallback]
public class FLinearConstraint : FConstraintBaseParams
{
    public float Limit;
    public ELinearConstraintMotion XMotion;
    public ELinearConstraintMotion YMotion;
    public ELinearConstraintMotion ZMotion;
    
    
    public FLinearConstraint(FStructFallback fallback) : base(fallback)
    {
        Limit = fallback.GetOrDefault<float>(nameof(Limit));
        XMotion = fallback.GetOrDefault<ELinearConstraintMotion>(nameof(XMotion));
        YMotion = fallback.GetOrDefault<ELinearConstraintMotion>(nameof(YMotion));
        ZMotion = fallback.GetOrDefault<ELinearConstraintMotion>(nameof(ZMotion));
    }
}

public enum ELinearConstraintMotion
{
    LCM_Free,
    LCM_Limited,
    LCM_Locked,
    LCM_MAX,
}

[StructFallback]
public class FConeConstraint  : FConstraintBaseParams
{
    public float Swing1LimitDegrees;
    public float Swing2LimitDegrees;
    public EAngularConstraintMotion Swing1Motion;
    public EAngularConstraintMotion Swing2Motion;
    
    
    public FConeConstraint(FStructFallback fallback) : base(fallback)
    {
        Swing1LimitDegrees = fallback.GetOrDefault<float>(nameof(Swing1LimitDegrees));
        Swing2LimitDegrees = fallback.GetOrDefault<float>(nameof(Swing2LimitDegrees));
        Swing1Motion = fallback.GetOrDefault<EAngularConstraintMotion>(nameof(Swing1Motion));
        Swing2Motion = fallback.GetOrDefault<EAngularConstraintMotion>(nameof(Swing2Motion));
    }
}

[StructFallback]
public class FTwistConstraint  : FConstraintBaseParams
{
    public float TwistLimitDegrees;
    public EAngularConstraintMotion TwistMotion;
    
    
    public FTwistConstraint(FStructFallback fallback) : base(fallback)
    {
        TwistLimitDegrees = fallback.GetOrDefault<float>(nameof(TwistLimitDegrees));
        TwistMotion = fallback.GetOrDefault<EAngularConstraintMotion>(nameof(TwistMotion));
    }
}

public enum EAngularConstraintMotion
{
    ACM_Free,
    ACM_Limited,
    ACM_Locked,
    ACM_MAX,
}