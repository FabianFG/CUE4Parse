using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.UE4.Objects.PhysicsEngine;

[StructFallback]
public readonly struct FBodyInstance : IUStruct
{
    public readonly bool bSimulatePhysics;
    public readonly bool bOverrideMass;
    public readonly bool bEnableGravity = true;
    public readonly bool bUpdateKinematicFromSimulation;
    public readonly bool bGyroscopicTorqueEnabled;
    public readonly bool bAutoWeld;
    public readonly bool bStartAwake = true;
    public readonly bool bGenerateWakeEvents;
    public readonly bool bUpdateMassWhenScaleChanges; // cvar p.UseDeprecatedBehaviorUpdateMassScaleChanges

    public readonly byte PositionSolverIterationCount = 8;
    public readonly byte VelocitySolverIterationCount = 2;
    public readonly byte ProjectionSolverIterationCount = 1;
    public readonly ECollisionChannel ObjectType = ECollisionChannel.ECC_WorldStatic;
    public readonly ECollisionEnabled CollisionEnabled = ECollisionEnabled.QueryAndPhysics;

    public FBodyInstance(FStructFallback fallback)
    {
        bSimulatePhysics = fallback.GetOrDefault<byte>(nameof(bSimulatePhysics)) != 0;
        bOverrideMass = fallback.GetOrDefault<byte>(nameof(bOverrideMass)) != 0;
        bEnableGravity = fallback.GetOrDefault<byte>(nameof(bEnableGravity), 1) != 0;
        bUpdateKinematicFromSimulation = fallback.GetOrDefault<byte>(nameof(bUpdateKinematicFromSimulation)) != 0;
        bGyroscopicTorqueEnabled = fallback.GetOrDefault<byte>(nameof(bGyroscopicTorqueEnabled)) != 0;
        bAutoWeld = fallback.GetOrDefault<byte>(nameof(bAutoWeld)) != 0;
        bStartAwake = fallback.GetOrDefault<byte>(nameof(bStartAwake), 1) != 0;
        bGenerateWakeEvents = fallback.GetOrDefault<byte>(nameof(bGenerateWakeEvents)) != 0;
        bUpdateMassWhenScaleChanges = fallback.GetOrDefault<byte>(nameof(bUpdateMassWhenScaleChanges)) != 0;

        PositionSolverIterationCount = fallback.GetOrDefault(nameof(PositionSolverIterationCount), PositionSolverIterationCount);
        VelocitySolverIterationCount = fallback.GetOrDefault(nameof(VelocitySolverIterationCount), VelocitySolverIterationCount);
        ProjectionSolverIterationCount = fallback.GetOrDefault(nameof(ProjectionSolverIterationCount), ProjectionSolverIterationCount);
        ObjectType = fallback.GetOrDefault(nameof(ObjectType), ObjectType);
        CollisionEnabled = fallback.GetOrDefault(nameof(CollisionEnabled), CollisionEnabled);
    }
}
