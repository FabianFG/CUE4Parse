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
        bSimulatePhysics = fallback.GetOrDefault(nameof(bSimulatePhysics), bSimulatePhysics);
        bOverrideMass = fallback.GetOrDefault(nameof(bOverrideMass), bOverrideMass);
        bEnableGravity = fallback.GetOrDefault(nameof(bEnableGravity), bEnableGravity);
        bUpdateKinematicFromSimulation = fallback.GetOrDefault(nameof(bUpdateKinematicFromSimulation), bUpdateKinematicFromSimulation);
        bGyroscopicTorqueEnabled = fallback.GetOrDefault(nameof(bGyroscopicTorqueEnabled), bGyroscopicTorqueEnabled);
        bAutoWeld = fallback.GetOrDefault(nameof(bAutoWeld), bAutoWeld);
        bStartAwake = fallback.GetOrDefault(nameof(bStartAwake), bStartAwake);
        bGenerateWakeEvents = fallback.GetOrDefault(nameof(bGenerateWakeEvents), bGenerateWakeEvents);
        bUpdateMassWhenScaleChanges = fallback.GetOrDefault(nameof(bUpdateMassWhenScaleChanges), bUpdateMassWhenScaleChanges);

        PositionSolverIterationCount = fallback.GetOrDefault(nameof(PositionSolverIterationCount), PositionSolverIterationCount);
        VelocitySolverIterationCount = fallback.GetOrDefault(nameof(VelocitySolverIterationCount), VelocitySolverIterationCount);
        ProjectionSolverIterationCount = fallback.GetOrDefault(nameof(ProjectionSolverIterationCount), ProjectionSolverIterationCount);
        ObjectType = fallback.GetOrDefault(nameof(ObjectType), ObjectType);
        CollisionEnabled = fallback.GetOrDefault(nameof(CollisionEnabled), CollisionEnabled);
    }
}
