using CUE4Parse.UE4.Assets.Exports.Component.Atmosphere;
using CUE4Parse.UE4.Assets.Exports.Component.Landscape;
using CUE4Parse.UE4.Assets.Exports.Component.Lights;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component;

public class UActorComponent : UObject
{
    [JsonIgnore] public FSimpleMemberReference[]? UCSModifiedProperties;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Position == validPos) // I think after validpos all read default to dummy data 000000s
            return;

        if (Ar.Game is EGame.GAME_SuicideSquad) Ar.Position += 4;
        if (Ar.Game == EGame.GAME_WorldofJadeDynasty) Ar.Position += 16;

        if (FFortniteReleaseBranchCustomObjectVersion.Get(Ar) >= FFortniteReleaseBranchCustomObjectVersion.Type.ActorComponentUCSModifiedPropertiesSparseStorage)
        {
            UCSModifiedProperties = Ar.ReadArray(() => new FSimpleMemberReference(Ar));
        }
    }
}

public struct FSimpleMemberReference(FAssetArchive Ar)
{
    public FPackageIndex MemberParent = new FPackageIndex(Ar);
    public FName MemberName = Ar.ReadFName();
    public FGuid MemberGuid = Ar.Read<FGuid>();
}

public class UAIPerceptionComponent : UActorComponent;
public class UAIPerceptionStimuliSourceComponent : UActorComponent;
public class UActorSequenceComponent : UActorComponent;
public class UActorTextureStreamingBuildDataComponent : UActorComponent;
public class UApplicationLifecycleComponent : UActorComponent;
public class UArchVisCharMovementComponent : UCharacterMovementComponent;
public class UArrowComponent : UPrimitiveComponent;
public class UAsyncPhysicsInputComponent : UActorComponent;
public class UAtmosphericFogComponent : USkyAtmosphereComponent;
public class UAudioCaptureComponent : USynthComponent;
public class UAudioComponent : USceneComponent;
public class UAudioCurveSourceComponent : UAudioComponent;
public class UAxisGizmoHandleGroup : UGizmoHandleGroup;
public class UBaseDynamicMeshComponent : UMeshComponent;
public class UBasic2DLineSetComponent : UBasicLineSetComponentBase;
public class UBasic2DPointSetComponent : UBasicPointSetComponentBase;
public class UBasic2DTriangleSetComponent : UBasicTriangleSetComponentBase;
public class UBasic3DLineSetComponent : UBasicLineSetComponentBase;
public class UBasic3DPointSetComponent : UBasicPointSetComponentBase;
public class UBasic3DTriangleSetComponent : UBasicTriangleSetComponentBase;
public class UBasicLineSetComponentBase : UMeshComponent;
public class UBasicPointSetComponentBase : UMeshComponent;
public class UBasicTriangleSetComponentBase : UMeshComponent;
public class UBehaviorTreeComponent : UBrainComponent;
public class UBillboardComponent : UPrimitiveComponent;
public class UBlackboardComponent : UActorComponent;
public class UBoundsCopyComponent : UActorComponent;
public class UBoxComponent : UShapeComponent;
public class UBoxFalloff : UFieldNodeFloat;
public class UBoxReflectionCaptureComponent : UReflectionCaptureComponent;
public class UBrainComponent : UActorComponent;
public class UBrushComponent : UPrimitiveComponent;
public class UCableComponent : UMeshComponent;
public class UCameraComponent : USceneComponent;
public class UCameraShakeSourceComponent : USceneComponent;
public class UCapsuleComponent : UShapeComponent;
public class UChaosDebugDrawComponent : UActorComponent;
public class UChaosDestructionListener : USceneComponent;
public class UChaosEventListenerComponent : UActorComponent;
public class UChaosGameplayEventDispatcher : UChaosEventListenerComponent;
public class UChaosVDInstancedStaticMeshComponent : UInstancedStaticMeshComponent;
public class UChaosVDParticleDataComponent : UActorComponent;
public class UChaosVDSceneQueryDataComponent : UActorComponent;
public class UChaosVDSolverCollisionDataComponent : UActorComponent;
public class UChaosVDSolverJointConstraintDataComponent : UActorComponent;
public class UChaosVDStaticMeshComponent : UStaticMeshComponent;
public class UCharacterMovementComponent : UPawnMovementComponent;
public class UChildActorComponent : USceneComponent;
public class UCineCameraComponent : UCameraComponent;
public class UClusterUnionComponent : UPrimitiveComponent;
public class UClusterUnionReplicatedProxyComponent : UActorComponent;
public class UComputeGraphComponent : UActorComponent;
public class UControlPointMeshComponent : UStaticMeshComponent;
public class UControlRigComponent : UPrimitiveComponent;
public class UControlRigSkeletalMeshComponent : UDebugSkelMeshComponent;
public class UCrowdFollowingComponent : UPathFollowingComponent;
public class UCullingField : UFieldNodeBase;
public class UCustomMeshComponent : UMeshComponent;
public class UDataflowComponent : UPrimitiveComponent;
public class UDebugDrawComponent : UPrimitiveComponent;
public class UDebugSkelMeshComponent : USkeletalMeshComponent;
public class UDefaultPawnMovement : UFloatingPawnMovement;
public class UDrawFrustumComponent : UPrimitiveComponent;
public class UDrawSphereComponent : USphereComponent;
public class UDynamicMeshComponent : UBaseDynamicMeshComponent;
public class UEQSRenderingComponent : UDebugDrawComponent;
public class UEditorAutomationActorComponent : UEditorUtilityActorComponent;
public class UEditorUtilityActorComponent : UActorComponent;
public class UEnhancedInputComponent : UInputComponent;
public class UEnvelopeFollowerListener : UActorComponent;
public class UExponentialHeightFogComponent : USceneComponent;
public class UFXSystemComponent : UPrimitiveComponent;
public class UFieldNodeBase : UActorComponent;
public class UFieldNodeFloat : UFieldNodeBase;
public class UFieldNodeInt : UFieldNodeBase;
public class UFieldNodeVector : UFieldNodeBase;
public class UFieldSystemComponent : UPrimitiveComponent;
public class UFieldSystemMetaData : UActorComponent;
public class UFieldSystemMetaDataFilter : UFieldSystemMetaData;
public class UFieldSystemMetaDataIteration : UFieldSystemMetaData;
public class UFieldSystemMetaDataProcessingResolution : UFieldSystemMetaData;
public class UFloatingPawnMovement : UPawnMovementComponent;
public class UForceFeedbackComponent : USceneComponent;
public class UFuncTestRenderingComponent : UPrimitiveComponent;
public class UGameplayCameraComponent : USceneComponent;
public class UGameplayCameraSystemComponent : USceneComponent;
public class UGameplayDebuggerRenderingComponent : UDebugDrawComponent;
public class UGameplayTasksComponent : UActorComponent;
public class UGeometryCacheComponent : UMeshComponent;
public class UGeometryCollectionComponent : UMeshComponent;
public class UGeometryCollectionDebugDrawComponent : UActorComponent;
public class UGeometryCollectionISMPoolComponent : USceneComponent;
public class UGeometryCollectionISMPoolDebugDrawComponent : UDebugDrawComponent;
public class UGizmoArrowComponent : UGizmoBaseComponent;
public class UGizmoBaseComponent : UPrimitiveComponent;
public class UGizmoBoxComponent : UGizmoBaseComponent;
public class UGizmoCircleComponent : UGizmoBaseComponent;
public class UGizmoHandleGroup : USceneComponent;
public class UGizmoHandleMeshComponent : UStaticMeshComponent;
public class UGizmoLineHandleComponent : UGizmoBaseComponent;
public class UGizmoRectangleComponent : UGizmoBaseComponent;
public class UGranularSynth : USynthComponent;
public class UGrassInstancedStaticMeshComponent : UHierarchicalInstancedStaticMeshComponent;
public class UGridPathFollowingComponent : UPathFollowingComponent;
public class UGroomComponent : UMeshComponent;
public class UHLODInstancedStaticMeshComponent : UInstancedStaticMeshComponent;
public class UHairStrandsComponent : UGroomComponent;
public class UHeterogeneousVolumeComponent : UMeshComponent;
public class UIKRigComponent : UActorComponent;
public class UImgMediaPlaybackComponent : UActorComponent;
public class UInputComponent : UActorComponent;
public class UInteractiveFoliageComponent : UStaticMeshComponent;
public class UInterpToMovementComponent : UMovementComponent;
public class ULODSyncComponent : UActorComponent;
public class ULandscapeGizmoRenderComponent : UPrimitiveComponent;
public class ULandscapeMeshCollisionComponent : ULandscapeHeightfieldCollisionComponent;
public class ULandscapeMeshProxyComponent : UStaticMeshComponent;
public class ULandscapeNaniteComponent : UStaticMeshComponent;
public class ULandscapeSplinesComponent : UPrimitiveComponent;
public class ULevelInstanceComponent : USceneComponent;
public class ULightmassPortalComponent : USceneComponent;
public class ULineBatchComponent : UPrimitiveComponent;
public class ULineSetComponent : UMeshComponent;
public class ULocalFogVolumeComponent : USceneComponent;
public class UMRMeshComponent : UPrimitiveComponent;
public class UMaterialBillboardComponent : UPrimitiveComponent;
public class UMaterialEditorMeshComponent : UStaticMeshComponent;
public class UMaterialSpriteComponent : UMaterialBillboardComponent;
public class UMediaComponent : UActorComponent;
public class UMediaPlateComponent : UActorComponent;
public class UMediaSoundComponent : USynthComponent;
public class UMeshComponent : UPrimitiveComponent;
public class UWaterBodyComponent : UPrimitiveComponent;
public class UWaterMeshComponent : UMeshComponent;
public class UMeshWireframeComponent : UMeshComponent;
public class UMockDataMeshTrackerComponent : USceneComponent;
public class UMockGameplayTasksComponent : UGameplayTasksComponent;
public class UModularSynthComponent : USynthComponent;
public class UMotionControllerComponent : UPrimitiveComponent;
public class UMovementComp_Character : UCharacterMovementComponent;
public class UMovementComp_Projectile : UProjectileMovementComponent;
public class UMovementComp_Rotating : URotatingMovementComponent;
public class UMovementComponent : UActorComponent;
public class UNavLinkComponent : UPrimitiveComponent;
public class UNavLinkCustomComponent : UNavRelevantComponent;
public class UNavLinkRenderingComponent : UPrimitiveComponent;
public class UNavMeshRenderingComponent : UDebugDrawComponent;
public class UNavModifierComponent : UNavRelevantComponent;
public class UNavMovementComponent : UMovementComponent;
public class UNavRelevantComponent : UActorComponent;
public class UNavTestRenderingComponent : UDebugDrawComponent;
public class UNavigationGraphNodeComponent : USceneComponent;
public class UNavigationInvokerComponent : UActorComponent;
public class UNetworkPhysicsComponent : UActorComponent;
public class UNetworkPhysicsSettingsComponent : UActorComponent;
public class UNiagaraComponent : UFXSystemComponent;
public class UNiagaraCullProxyComponent : UNiagaraComponent;
public class UNoiseField : UFieldNodeFloat;
public class UOctaneLightSettingsOverride : UOctaneOverrideComponent;
public class UOctaneNodeComponent : UActorComponent;
public class UOctaneObjectLayerComponent : USceneComponent;
public class UOctaneOrbxComponent : USceneComponent;
public class UOctaneOverrideComponent : USceneComponent;
public class UOctreeDynamicMeshComponent : UBaseDynamicMeshComponent;
public class UOperatorField : UFieldNodeBase;
public class UPaperAnimatedRenderComponent : UPaperFlipbookComponent;
public class UPaperFlipbookComponent : UMeshComponent;
public class UPaperGroupedSpriteComponent : UMeshComponent;
public class UPaperRenderComponent : UPaperSpriteComponent;
public class UPaperSpriteComponent : UMeshComponent;
public class UPaperTerrainComponent : UPrimitiveComponent;
public class UPaperTerrainSplineComponent : USplineComponent;
public class UPaperTileMapComponent : UMeshComponent;
public class UPaperTileMapRenderComponent : UPaperTileMapComponent;

public class UParticleSystemComponent : UFXSystemComponent
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if(Ar.Game == EGame.GAME_WorldofJadeDynasty) Ar.Position += 16;
        base.Deserialize(Ar, validPos);
    }
}

public class UParticleSystem : UObject
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if(Ar.Game == EGame.GAME_WorldofJadeDynasty) Ar.Position += 8;
        base.Deserialize(Ar, validPos);
    }
}

public class UPathFollowingComponent : UActorComponent;
public class UPawnActionsComponent : UActorComponent;
public class UPawnMovementComponent : UNavMovementComponent;
public class UPawnNoiseEmitterComponent : UActorComponent;
public class UPawnSensingComponent : UActorComponent;
public class UPhysicalAnimationComponent : UActorComponent;
public class UPhysicsConstraintComponent : USceneComponent;
public class UPhysicsFieldComponent : USceneComponent;
public class UPhysicsHandleComponent : UActorComponent;
public class UPhysicsSpringComponent : USceneComponent;
public class UPhysicsThrusterComponent : USceneComponent;
public class UPivotPlaneTranslationGizmoHandleGroup : UAxisGizmoHandleGroup;
public class UPivotRotationGizmoHandleGroup : UAxisGizmoHandleGroup;
public class UPivotScaleGizmoHandleGroup : UAxisGizmoHandleGroup;
public class UPivotTranslationGizmoHandleGroup : UAxisGizmoHandleGroup;
public class UPlanarReflectionComponent : USceneCaptureComponent;
public class UPlaneFalloff : UFieldNodeFloat;
public class UPlaneReflectionCaptureComponent : UReflectionCaptureComponent;
public class UPlatformEventsComponent : UActorComponent;
public class UPointSetComponent : UMeshComponent;
public class UPoseableMeshComponent : USkinnedMeshComponent;
public class UPostProcessComponent : USceneComponent;
public class UProceduralFoliageComponent : UActorComponent;
public class UProceduralMeshComponent : UMeshComponent;
public class UProjectileMovementComponent : UMovementComponent;
public class URB_ConstraintComponent : UPhysicsConstraintComponent;
public class URB_Handle : UPhysicsHandleComponent;
public class URB_RadialForceComponent : URadialForceComponent;
public class URB_ThrusterComponent : UPhysicsThrusterComponent;
public class URadialFalloff : UFieldNodeFloat;
public class URadialForceComponent : USceneComponent;
public class URadialIntMask : UFieldNodeInt;
public class URadialVector : UFieldNodeVector;
public class URandomVector : UFieldNodeVector;
public class UReflectionCaptureComponent : USceneComponent;
public class UReturnResultsTerminal : UFieldNodeBase;
public class URotatingMovementComponent : UMovementComponent;
public class URuntimeVirtualTextureComponent : USceneComponent;
public class USceneCaptureComponent : USceneComponent;
public class USceneCaptureComponent2D : USceneCaptureComponent;
public class USceneCaptureComponentCube : USceneCaptureComponent;
public class USensingComponent : UPawnSensingComponent;
public class UShapeComponent : UPrimitiveComponent;
public class USingleAnimSkeletalComponent : USkeletalMeshComponent;
public class USkeletalMeshReplicatedComponent : USkeletalMeshComponent;
public class USkinnedMeshComponent : UMeshComponent;
public class USkyLightComponent : ULightComponentBase;
public class USmartNavLinkComponent : UNavLinkCustomComponent;
public class USparseVolumeTextureViewerComponent : UPrimitiveComponent;
public class USpectatorPawnMovement : UFloatingPawnMovement;
public class USphereComponent : UShapeComponent;
public class USphereReflectionCaptureComponent : UReflectionCaptureComponent;
public class USplineComponent : UPrimitiveComponent;
public class USplineNavModifierComponent : UNavModifierComponent;
public class USpringArmComponent : USceneComponent;
public class USpriteComponent : UBillboardComponent;
public class UStaticMeshReplicatedComponent : UStaticMeshComponent;
public class UStereoLayerComponent : USceneComponent;
public class UStretchGizmoHandleGroup : UGizmoHandleGroup;
public class USynthComponent : USceneComponent;
public class USynthComponentMonoWaveTable : USynthComponent;
public class USynthComponentToneGenerator : USynthComponent;
public class USynthSamplePlayer : USynthComponent;
public class UTestPhaseComponent : USceneComponent;
public class UTextRenderComponent : UPrimitiveComponent;
public class UTimelineComponent : UActorComponent;
public class UToFloatField : UFieldNodeFloat;
public class UToIntegerField : UFieldNodeInt;
public class UTriangleSetComponent : UMeshComponent;
public class UUniformInteger : UFieldNodeInt;
public class UUniformScalar : UFieldNodeFloat;
public class UUniformScaleGizmoHandleGroup : UGizmoHandleGroup;
public class UUniformVector : UFieldNodeVector;
public class UVOIPTalker : UActorComponent;
public class UVREditorCameraWidgetComponent : UVREditorWidgetComponent;
public class UVREditorWidgetComponent : UWidgetComponent;
public class UVectorFieldComponent : UPrimitiveComponent;
public class UViewportDragOperationComponent : UActorComponent;
public class UVisualLoggerRenderingComponent : UDebugDrawComponent;
public class UVoipListenerSynthComponent : USynthComponent;
public class UVolumetricCloudComponent : USceneComponent;
public class UWaveScalar : UFieldNodeFloat;
public class UWidgetComponent : UMeshComponent;
public class UWidgetInteractionComponent : USceneComponent;
public class UWindDirectionalSourceComponent : USceneComponent;
public class UWorldPartitionDestructibleHLODComponent : USceneComponent;
public class UWorldPartitionDestructibleHLODMeshComponent : UWorldPartitionDestructibleHLODComponent;
public class UWorldPartitionStreamingSourceComponent : UActorComponent;
