using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions
{
    // Custom serialization version for changes made in //UE5/Main stream
    public static class FUE5MainStreamObjectVersion
    {
        public enum Type
        {
		    // Before any version changes were made
		    BeforeCustomVersionWasAdded = 0,

		    // Nanite data added to Chaos geometry collections
		    GeometryCollectionNaniteData,

		    // Nanite Geometry Collection data moved to DDC
		    GeometryCollectionNaniteDDC,

		    // Removing SourceAnimationData, animation layering is now applied during compression
		    RemovingSourceAnimationData,

		    // New MeshDescription format.
		    // This is the correct versioning for MeshDescription changes which were added to ReleaseObjectVersion.
		    MeshDescriptionNewFormat,

		    // Serialize GridGuid in PartitionActorDesc
		    PartitionActorDescSerializeGridGuid,

		    // Set PKG_ContainsMapData on external actor packages
		    ExternalActorsMapDataPackageFlag,

		    // Added a new configurable BlendProfileMode that the user can setup to control the behavior of blend profiles.
		    AnimationAddedBlendProfileModes,

		    // Serialize DataLayers in WorldPartitionActorDesc
		    WorldPartitionActorDescSerializeDataLayers,

		    // Renaming UAnimSequence::NumFrames to NumberOfKeys, as that what is actually contains.
		    RenamingAnimationNumFrames,

		    // Serialize HLODLayer in WorldPartition HLODActorDesc
		    WorldPartitionHLODActorDescSerializeHLODLayer,

		    // Fixed Nanite Geometry Collection cooked data
		    GeometryCollectionNaniteCooked,

		    // Added bCooked to UFontFace assets
		    AddedCookedBoolFontFaceAssets,

		    // Serialize CellHash in WorldPartition HLODActorDesc
		    WorldPartitionHLODActorDescSerializeCellHash,

		    // Nanite data is now transient in Geometry Collection similar to how RenderData is transient in StaticMesh.
		    GeometryCollectionNaniteTransient,

		    // Added FLandscapeSplineActorDesc
		    AddedLandscapeSplineActorDesc,

		    // Added support for per-object collision constraint flag. [Chaos]
		    AddCollisionConstraintFlag,

		    // Initial Mantle Serialize Version
		    MantleDbSerialize,

		    // Animation sync groups explicitly specify sync method
		    AnimSyncGroupsExplicitSyncMethod,

		    // Fixup FLandscapeActorDesc Grid indices
		    FLandscapeActorDescFixupGridIndices,

		    // FoliageType with HLOD support
		    FoliageTypeIncludeInHLOD,

		    // Introducing UAnimDataModel sub-object for UAnimSequenceBase containing all animation source data
		    IntroducingAnimationDataModel,

		    // Serialize ActorLabel in WorldPartitionActorDesc
		    WorldPartitionActorDescSerializeActorLabel,

		    // Fix WorldPartitionActorDesc serialization archive not persistent
		    WorldPartitionActorDescSerializeArchivePersistent,

		    // Fix potentially duplicated actors when using ForceExternalActorLevelReference
		    FixForceExternalActorLevelReferenceDuplicates,

		    // Make UMeshDescriptionBase serializable
		    SerializeMeshDescriptionBase,

		    // Chaos FConvex uses array of FVec3s for vertices instead of particles
		    ConvexUsesVerticesArray,

		    // Serialize HLOD info in WorldPartitionActorDesc
		    WorldPartitionActorDescSerializeHLODInfo,

		    // Expose particle Disabled flag to the game thread
		    AddDisabledFlag,

		    // Moving animation custom attributes from AnimationSequence to UAnimDataModel
		    MoveCustomAttributesToDataModel,

		    // Use of triangulation at runtime in BlendSpace
		    BlendSpaceRuntimeTriangulation,

		    // Fix to the Cubic smoothing, plus introduction of new smoothing types
		    BlendSpaceSmoothingImprovements,

		    // Removing Tessellation parameters from Materials
		    RemovingTessellationParameters,

		    // Sparse class data serializes its associated structure to allow for BP types to be used
		    SparseClassDataStructSerialization,

		    // PackedLevelInstance bounds fix
		    PackedLevelInstanceBoundsFix,

		    // Initial set of anim nodes converted to use constants held in sparse class data
		    AnimNodeConstantDataRefactorPhase0,

		    // Explicitly serialized bSavedCachedExpressionData for Material(Instance)
		    MaterialSavedCachedData,

		    // Remove explicit decal blend mode
		    RemoveDecalBlendMode,

		    // Made directional lights be atmosphere lights by default
		    DirLightsAreAtmosphereLightsByDefault,

		    // Changed how world partition streaming cells are named
		    WorldPartitionStreamingCellsNamingShortened,

		    // Changed how actor descriptors compute their bounds
		    WorldPartitionActorDescGetStreamingBounds,

		    // Switch FMeshDescriptionBulkData to use virtualized bulkdata
		    MeshDescriptionVirtualization,

		    // Switch FTextureSource to use virtualized bulkdata
		    TextureSourceVirtualization,

		    // RigVM to store more information alongside the Copy Operator
		    RigVMCopyOpStoreNumBytes,

		    // Expanded separate translucency into multiple passes
		    MaterialTranslucencyPass,

		    // Chaos FGeometryCollectionObject user defined collision shapes support
		    GeometryCollectionUserDefinedCollisionShapes,

		    // Removed the AtmosphericFog component with conversion to SkyAtmosphere component
		    RemovedAtmosphericFog,

		    // The SkyAtmosphere now light up the heightfog by default, and by default the height fog has a black color.
		    SkyAtmosphereAffectsHeightFogWithBetterDefault,

		    // Ordering of samples in BlendSpace
		    BlendSpaceSampleOrdering,

		    // No longer bake MassToLocal transform into recorded transform data in GeometryCollection caching
		    GeometryCollectionCacheRemovesMassToLocal,

		    // UEdGraphPin serializes SourceIndex
		    EdGraphPinSourceIndex,

		    // Change texture bulkdatas to have unique guids
		    VirtualizedBulkDataHaveUniqueGuids,

		    // Introduce RigVM Memory Class Object
		    RigVMMemoryStorageObject,

		    // Ray tracing shadows have three states now (Disabled, Use Project Settings, Enabled)
		    RayTracedShadowsType,

		    // Add bVisibleInRayTracing flag to Skeletal Mesh Sections
		    SkelMeshSectionVisibleInRayTracingFlagAdded,

		    // Add generic tagging of all anim graph nodes in anim blueprints
		    AnimGraphNodeTaggingAdded,

		    // Add custom version to FDynamicMesh3
		    DynamicMeshCompactedSerialization,

		    // Remove the inline reduction bulkdata and replace it by a simple vertex and triangle count cache
		    ConvertReductionBaseSkeletalMeshBulkDataToInlineReductionCacheData,

		    // Added some new MeshInfo to the FSkeletalMeshLODModel class.
		    SkeletalMeshLODModelMeshInfo,

		    // Add Texture DoScaleMipsForAlphaCoverage
		    TextureDoScaleMipsForAlphaCoverage,

		    // Fixed default value of volumetric cloud to be exact match with main view, more expenssive but we let user choosing how to lower the quality.
		    VolumetricCloudReflectionSampleCountDefaultUpdate,

		    // Use special BVH for TriangleMesh, instead of the AABBTree
		    UseTriangleMeshBVH,

		    // FDynamicMeshAttributeSet has Weight Maps. TDynamicAttributeBase serializes its name.
		    DynamicMeshAttributesWeightMapsAndNames,

		    // Switching FK control naming scheme to incorporate _CURVE for curve controls
		    FKControlNamingScheme,

		    // Fix-up for FRichCurveKey::TangentWeightMode, which were found to contain invalid value w.r.t the enum-type
		    RichCurveKeyInvalidTangentMode,

		    // Enforcing new automatic tangent behaviour, enforcing auto-tangents for Key0 and KeyN to be flat, for Animation Assets.
		    ForceUpdateAnimationAssetCurveTangents,

		    // SoundWave Update to use EditorBuildData for it's RawData
		    SoundWaveVirtualizationUpdate,

		    // Fix material feature level nodes to account for new SM6 input pin.
		    MaterialFeatureLevelNodeFixForSM6,

		    // Fix material feature level nodes to account for new SM6 input pin.
		    GeometryCollectionPerChildDamageThreshold,

		    // Move some Chaos flags into a bitfield
		    AddRigidParticleControlFlags,

		    // Allow each LiveLink controller to specify its own component to control
		    LiveLinkComponentPickerPerController,

		    // Remove Faces in Triangle Mesh BVH
		    RemoveTriangleMeshBVHFaces,

		    // Moving all nodal offset handling to Lens Component
		    LensComponentNodalOffset,

		    // GPU none interpolated spawning no longer calls the update script
		    FixGpuAlwaysRunningUpdateScriptNoneInterpolated,

		    // World partition streaming policy serialization only for cooked builds
		    WorldPartitionSerializeStreamingPolicyOnCook,

		    // Remove serialization of bounds relevant from  WorldPartitionActorDesc
		    WorldPartitionActorDescRemoveBoundsRelevantSerialization,

		    // Added IAnimationDataModel interface and replace UObject based representation for Animation Assets
		    // This version had to be undone. Animation assets saved between this and the subsequent backout version
		    // will be unable to be loaded
		    AnimationDataModelInterface_BackedOut,

		    // Deprecate LandscapeSplineActorDesc
		    LandscapeSplineActorDescDeprecation,

		    // Revert the IAnimationDataModel changes. Animation assets
		    BackoutAnimationDataModelInterface,

		    // Made stationary local and skylights behave similar to SM5
		    MobileStationaryLocalLights,

		    // Made ManagedArrayCollection::FValueType::Value always serialize when FValueType is
		    ManagedArrayCollectionAlwaysSerializeValue,

		    // Moving all distortion handling to Lens Component
		    LensComponentDistortion,

		    // Updated image media source path resolution logic
		    ImgMediaPathResolutionWithEngineOrProjectTokens,

		    // Add low resolution data in Height Field
		    AddLowResolutionHeightField,

		    // Low resolution data in Height Field will store one height for (6x6) 36 cells
		    DecreaseLowResolutionHeightField,

		    // Add damage propagation settings to geometry collections
		    GeometryCollectionDamagePropagationData,

		    // Wheel friction forces are now applied at tire contact point
		    VehicleFrictionForcePositionChange,

		    // Add flag to override MeshDeformer on a SkinnedMeshComponent.
		    AddSetMeshDeformerFlag,

		    // Replace FNames for class/actor paths with FSoftObjectPath
		    WorldPartitionActorDescActorAndClassPaths,

		    // Reintroducing AnimationDataModelInterface_BackedOut changes
		    ReintroduceAnimationDataModelInterface,

		    // Support 16-bit skin weights on SkeletalMesh
		    IncreasedSkinWeightPrecision,

		    // bIsUsedWithVolumetricCloud flag auto conversion
		    MaterialHasIsUsedWithVolumetricCloudFlag,

		    // bIsUsedWithVolumetricCloud flag auto conversion
		    UpdateHairDescriptionBulkData,

		    // Added TransformScaleMethod pin to SpawnActorFromClass node
		    SpawnActorFromClassTransformScaleMethod,

		    // Added support for the RigVM to run branches lazily
		    RigVMLazyEvaluation,

		    // Adding additional object version to defer out-of-date pose asset warning until next resaves
		    PoseAssetRawDataGUIDUpdate,

		    // Store function information (and compilation data) in blueprint generated class
		    RigVMSaveFunctionAccessInModel,

		    // Store the RigVM execute context struct the VM uses in the archive
		    RigVMSerializeExecuteContextStruct,

		    // Store the Visual Logger timestamp as a double
		    VisualLoggerTimeStampAsDouble,

		    // Add ThinSurface instance override support
		    MaterialInstanceBasePropertyOverridesThinSurface,

		    // Add refraction mode None, converted from legacy when the refraction pin is not plugged.
		    MaterialRefractionModeNone,

		    // Store serialized graph function in the function data
		    RigVMSaveSerializedGraphInGraphFunctionData,

		    // Animation Sequence now stores its frame-rate on a per-platform basis
		    PerPlatformAnimSequenceTargetFrameRate,

		    // New default for number of attributes on 2d grids
		    NiagaraGrid2DDefaultUnnamedAttributesZero,

		    // RigVM generated class refactor
		    RigVMGeneratedClass,

		    // In certain cases, Blueprint pins with a PC_Object category would serialize a null PinSubCategoryObject
		    NullPinSubCategoryObjectFix,

		    // Allow custom event nodes to use access specifiers
		    AccessSpecifiersForCustomEvents,

		    // Explicit override of Groom's hair width
		    GroomAssetWidthOverride,

		    // Smart names removed from animation systems
		    AnimationRemoveSmartNames,

		    // Change the default for facing & alignment to be automatic
		    NiagaraSpriteRendererFacingAlignmentAutoDefault,

		    // Change the default for facing & alignment to be automatic
		    GroomAssetRemoveInAssetSerialization,

		    // Changed the material property connected bitmasks from 32bit to 64bit
		    IncreaseMaterialAttributesInputMask,

		    // Combines proprties into a new binding so users can select constant or binding
		    NiagaraSimStageNumIterationsBindings,

		    // Skeletal vertex attributes
		    SkeletalVertexAttributes,

		    // Store the RigVM execute context struct the VM uses in the archive
		    RigVMExternalExecuteContextStruct,

		    // serialization inputs and outputs as two different sections
		    DataflowSeparateInputOutputSerialization,

		    // Cloth collection tether initialization
		    ClothCollectionTetherInitialization,

		    // OpenColorIO transforms now serialize their generated texture(s) and shader code normally into the uasset.
		    OpenColorIOAssetCacheSerialization,

		    // Cloth collection single lod schema
		    ClothCollectionSingleLodSchema,

		    // -----<new versions can be added above this line>-------------------------------------------------
		    VersionPlusOne,
		    LatestVersion = VersionPlusOne - 1
        }

        public static readonly FGuid GUID = new(0x697DD581, 0xE64f41AB, 0xAA4A51EC, 0xBEB7B628);

        public static Type Get(FArchive Ar)
        {
            var ver = Ar.CustomVer(GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                < EGame.GAME_UE5_0 => Type.BeforeCustomVersionWasAdded,
                < EGame.GAME_UE5_1 => Type.TextureDoScaleMipsForAlphaCoverage,
                < EGame.GAME_UE5_2 => Type.WorldPartitionActorDescActorAndClassPaths,
                < EGame.GAME_UE5_3 => Type.RigVMGeneratedClass,
                _ => Type.LatestVersion
            };
        }
    }
}
