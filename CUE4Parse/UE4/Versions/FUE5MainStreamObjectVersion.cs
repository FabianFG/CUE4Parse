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
                _ => Type.LatestVersion
            };
        }
    }
}
