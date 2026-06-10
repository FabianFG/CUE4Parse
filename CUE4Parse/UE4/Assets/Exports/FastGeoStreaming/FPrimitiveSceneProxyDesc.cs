using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

public class FSceneProxyDesc
{
    public FPrimitiveSceneProxyDesc PrimitiveSceneProxyDesc;
    public FStaticMeshSceneProxyDesc? StaticMeshSceneProxyDesc;
    public FInstancedStaticMeshSceneProxyDesc? InstancedStaticMeshSceneProxyDesc;
    public FSkinnedMeshSceneProxyDesc? SkinnedMeshSceneProxyDesc;
    public FInstancedSkinnedMeshSceneProxyDesc? InstancedSkinnedMeshSceneProxyDesc;

    public FSceneProxyDesc() { }

    public FSceneProxyDesc(FFastGeoArchive Ar)
    {
        PrimitiveSceneProxyDesc = new FPrimitiveSceneProxyDesc(Ar);
    }
}

public struct FLightingChannels(FArchive Ar)
{
    public bool bChannel0 = Ar.ReadBoolean();
    public bool bChannel1 = Ar.ReadBoolean();
    public bool bChannel2 = Ar.ReadBoolean();
}

public class FPrimitiveSceneProxyDesc(FArchive Ar)
{
    public bool CastShadow = Ar.ReadBoolean();
    public bool bReceivesDecals = Ar.ReadBoolean();
    public bool bOnlyOwnerSee = Ar.ReadBoolean();
    public bool bOwnerNoSee = Ar.ReadBoolean();
    public bool bUseViewOwnerDepthPriorityGroup = Ar.ReadBoolean();
    public bool bVisibleInReflectionCaptures = Ar.ReadBoolean();
    public bool bVisibleInRealTimeSkyCaptures = Ar.ReadBoolean();
    public bool bVisibleInRayTracing = Ar.ReadBoolean();
    public bool bVisibleInReflections = Ar.Game >= EGame.GAME_UE5_8 && Ar.ReadBoolean();
    public bool bRenderInDepthPass = Ar.ReadBoolean();
    public bool bRenderInMainPass = Ar.ReadBoolean();
    public bool bTreatAsBackgroundForOcclusion = Ar.ReadBoolean();
    public bool bCastDynamicShadow = Ar.ReadBoolean();
    public bool bCastStaticShadow = Ar.ReadBoolean();
    public bool bEmissiveLightSource = Ar.ReadBoolean();
    public bool bAffectDynamicIndirectLighting = Ar.ReadBoolean();
    public bool bAffectIndirectLightingWhileHidden = Ar.ReadBoolean();
    public bool bAffectDistanceFieldLighting = Ar.ReadBoolean();
    public bool bCastVolumetricTranslucentShadow = Ar.ReadBoolean();
    public bool bCastContactShadow = Ar.ReadBoolean();
    public bool bCastHiddenShadow = Ar.ReadBoolean();
    public bool bCastShadowAsTwoSided = Ar.ReadBoolean();
    public bool bSelfShadowOnly = Ar.ReadBoolean();
    public bool bCastInsetShadow = Ar.ReadBoolean();
    public bool bCastCinematicShadow = Ar.ReadBoolean();
    public bool bCastFarShadow = Ar.ReadBoolean();
    public bool bLightAttachmentsAsGroup = Ar.ReadBoolean();
    public bool bSingleSampleShadowFromStationaryLights = Ar.ReadBoolean();
    public bool bUseAsOccluder = Ar.ReadBoolean();
    public bool bHasPerInstanceHitProxies = Ar.ReadBoolean();
    public bool bReceiveMobileCSMShadows = Ar.ReadBoolean();
    public bool bRenderCustomDepth = Ar.ReadBoolean();
    public bool bVisibleInSceneCaptureOnly = Ar.ReadBoolean();
    public bool bHiddenInSceneCapture = Ar.ReadBoolean();
    public bool bForceMipStreaming = Ar.ReadBoolean();
    public bool bRayTracingFarField = Ar.ReadBoolean();
    public bool bHoldout = Ar.ReadBoolean();
    public bool bIsFirstPerson = Ar.ReadBoolean();
    public bool bIsFirstPersonWorldSpaceRepresentation = Ar.ReadBoolean();
    public bool bCollisionEnabled = Ar.ReadBoolean();
    public bool bIsHidden = Ar.ReadBoolean();
    public bool bSupportsWorldPositionOffsetVelocity = Ar.ReadBoolean();
    public bool bIsInstancedStaticMesh = Ar.ReadBoolean();
    public bool bHasStaticLighting = Ar.ReadBoolean();
    public bool bHasValidSettingsForStaticLighting = Ar.ReadBoolean();
    public bool bIsPrecomputedLightingValid = Ar.ReadBoolean();
    public bool bShadowIndirectOnly = Ar.ReadBoolean();
    public EComponentMobility Mobility = Ar.Read<EComponentMobility>();
    public int TranslucencySortPriority = Ar.Read<int>();
    public float TranslucencySortDistanceOffset = Ar.Read<float>();
    public ELightmapType LightmapType = Ar.Read<ELightmapType>();
    public ESceneDepthPriorityGroup ViewOwnerDepthPriorityGroup = Ar.Read<ESceneDepthPriorityGroup>();
    public int CustomDepthStencilValue = Ar.Read<int>();
    public ERendererStencilMask CustomDepthStencilWriteMask = Ar.Read<ERendererStencilMask>();
    public FLightingChannels LightingChannels = new FLightingChannels(Ar);
    public ERayTracingGroupCullingPriority RayTracingGroupCullingPriority = Ar.Read<ERayTracingGroupCullingPriority>();
    public EIndirectLightingCacheQuality IndirectLightingCacheQuality = Ar.Read<EIndirectLightingCacheQuality>();
    public EShadowCacheInvalidationBehavior ShadowCacheInvalidationBehavior = Ar.Read<EShadowCacheInvalidationBehavior>();
    public ESceneDepthPriorityGroup DepthPriorityGroup = Ar.Read<ESceneDepthPriorityGroup>();
    public byte VirtualTextureLodBias = Ar.Read<byte>();
    public int VirtualTextureCullMips = Ar.Read<int>();
    public byte VirtualTextureMinCoverage = Ar.Read<byte>();
    public int VisibilityId = Ar.Read<int>();
    public float CachedMaxDrawDistance = Ar.Read<float>();
    public float MinDrawDistance = Ar.Read<float>();
    public float BoundsScale = Ar.Read<float>();
    public int RayTracingGroupId = Ar.Read<int>();
    public ERuntimeVirtualTextureMainPassType VirtualTextureRenderPassType = Ar.Read<ERuntimeVirtualTextureMainPassType>();
    public float VirtualTextureMainPassMaxDrawDistance = Ar.Read<float>();
}

public class FSkinnedMeshSceneProxyDesc(FFastGeoArchive Ar)// : FPrimitiveSceneProxyDesc
{
    public bool bForceWireframe = Ar.ReadBoolean();
    public bool bCanHighlightSelectedSections = Ar.ReadBoolean();
    public bool bRenderStatic = Ar.ReadBoolean();
    public bool bPerBoneMotionBlur = Ar.ReadBoolean();
    public bool bCastCapsuleDirectShadow = Ar.ReadBoolean();
    public bool bCastCapsuleIndirectShadow = Ar.ReadBoolean();
    public bool bCPUSkinning = Ar.ReadBoolean();
    public float StreamingDistanceMultiplier = Ar.Read<float>();
    public float NanitePixelProgrammableDistance = Ar.Read<float>();
    public float CapsuleIndirectShadowMinVisibility = Ar.Read<float>();
    public float OverlayMaterialMaxDrawDistance = Ar.Read<float>();
    public int PredictedLODLevel = Ar.Read<int>();
    public float MaxDistanceFactor = Ar.Read<float>();
    public FVector ComponentScale = new FVector(Ar);
    public FPackageIndex SkinnedAsset = Ar.ReadFPackageIndex();
    public FPackageIndex OverlayMaterial = Ar.ReadFPackageIndex();
    public FPackageIndex[] MaterialSlotsOverlayMaterial = Ar.ReadArray(Ar.ReadFPackageIndex);
}

public class FInstancedSkinnedMeshSceneProxyDesc(FFastGeoArchive Ar)// : FSkinnedMeshSceneProxyDesc
{
    public float AnimationMinScreenSize = Ar.Read<float>();
    public int InstanceMinDrawDistance = Ar.Read<int>();
    public int InstanceStartCullDistance = Ar.Read<int>();
    public int InstanceEndCullDistance = Ar.Read<int>();
}

public class FStaticMeshSceneProxyDesc
{
    public FPackageIndex StaticMesh;
    public FPackageIndex MeshPaintTexture;
    public FPackageIndex OverlayMaterial;
    public FPackageIndex[] MaterialSlotsOverlayMaterial;
    public float OverlayMaterialMaxDrawDistance;
    public int ForcedLodModel;
    public int MinLOD;
    public float WorldPositionOffsetDisableDistance;
    public float NanitePixelProgrammableDistance;
    public float DistanceFieldSelfShadowBias;
    public float DistanceFieldIndirectShadowMinVisibility;
    public int StaticLightMapResolution;
    public int MeshPaintTextureCoordinateIndex;
    public bool bReverseCulling;
    public bool bEvaluateWorldPositionOffset;
    public bool bOverrideMinLOD;
    public bool bCastDistanceFieldIndirectShadow;
    public bool bOverrideDistanceFieldSelfShadowBias;
    public bool bEvaluateWorldPositionOffsetInRayTracing;
    public bool bSortTriangles;
    public bool bDisallowNanite;
    public bool bForceDisableNanite;
    public bool bForceNaniteForMasked;

    public FStaticMeshSceneProxyDesc(FFastGeoArchive Ar)
    {
        StaticMesh = Ar.ReadFPackageIndex();
        if (Ar.Game is EGame.GAME_WutheringWavesFastGeo)
        {
            Ar.Position += 68;
            return;
        }
        MeshPaintTexture = Ar.Game >= EGame.GAME_UE5_8 ? Ar.ReadFPackageIndex() : new FPackageIndex();
        OverlayMaterial = Ar.ReadFPackageIndex();
        MaterialSlotsOverlayMaterial = Ar.ReadArray(Ar.ReadFPackageIndex);
        OverlayMaterialMaxDrawDistance = Ar.Read<float>();
        ForcedLodModel = Ar.Read<int>();
        MinLOD = Ar.Read<int>();
        WorldPositionOffsetDisableDistance = Ar.Read<float>();
        NanitePixelProgrammableDistance = Ar.Read<float>();
        DistanceFieldSelfShadowBias = Ar.Read<float>();
        DistanceFieldIndirectShadowMinVisibility = Ar.Read<float>();
        StaticLightMapResolution = Ar.Read<int>();
        MeshPaintTextureCoordinateIndex = Ar.Game >= EGame.GAME_UE5_8 ? Ar.Read<int>() : 0;
        bReverseCulling = Ar.ReadBoolean();
        bEvaluateWorldPositionOffset = Ar.ReadBoolean();
        bOverrideMinLOD = Ar.ReadBoolean();
        bCastDistanceFieldIndirectShadow = Ar.ReadBoolean();
        bOverrideDistanceFieldSelfShadowBias = Ar.ReadBoolean();
        bEvaluateWorldPositionOffsetInRayTracing = Ar.ReadBoolean();
        bSortTriangles = Ar.ReadBoolean();
        bDisallowNanite = Ar.ReadBoolean();
        bForceDisableNanite = Ar.ReadBoolean();
        bForceNaniteForMasked = Ar.ReadBoolean();
    }
}

public class FInstancedStaticMeshSceneProxyDesc(FFastGeoArchive Ar)// : FStaticMeshSceneProxyDesc
{
    public float InstanceLODDistanceScale = Ar.Read<float>();
    public int InstanceMinDrawDistance = Ar.Read<int>();
    public int InstanceStartCullDistance = Ar.Read<int>();
    public int InstanceEndCullDistance = Ar.Read<int>();
    public bool bUseGpuLodSelection = Ar.ReadBoolean();
}
