using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions
{
    // Custom serialization version for changes made in //UE5/Release-* stream
    public static class FUE5ReleaseStreamObjectVersion
    {
        public enum Type
        {
            // Before any version changes were made
            BeforeCustomVersionWasAdded = 0,

            // Added Lumen reflections to new reflection enum, changed defaults
            ReflectionMethodEnum,

            // Serialize HLOD info in WorldPartitionActorDesc
            WorldPartitionActorDescSerializeHLODInfo,

            // Removing Tessellation from materials and meshes.
            RemovingTessellation,

            // LevelInstance serialize runtime behavior
            LevelInstanceSerializeRuntimeBehavior,

            // Refactoring Pose Asset runtime data structures
            PoseAssetRuntimeRefactor,

            // Serialize the folder path of actor descs
            WorldPartitionActorDescSerializeActorFolderPath,

            // Change hair strands vertex format
            HairStrandsVertexFormatChange,

            // Added max linear and angular speed to Chaos bodies
            AddChaosMaxLinearAngularSpeed,

            // PackedLevelInstance version
            PackedLevelInstanceVersion,

            // PackedLevelInstance bounds fix
            PackedLevelInstanceBoundsFix,

            // Custom property anim graph nodes (linked anim graphs, control rig etc.) now use optional pin manager
            CustomPropertyAnimGraphNodesUseOptionalPinManager,

            // Add native double and int64 support to FFormatArgumentData
            TextFormatArgumentData64bitSupport,

            // Material layer stacks are no longer considered 'static parameters'
            MaterialLayerStacksAreNotParameters,

            // CachedExpressionData is moved from UMaterial to UMaterialInterface
            MaterialInterfaceSavedCachedData,

            // Add support for multiple cloth deformer LODs to be able to raytrace cloth with a different LOD than the one it is rendered with
            AddClothMappingLODBias,

            // Add support for different external actor packaging schemes
            AddLevelActorPackagingScheme,

            // Add support for linking to the attached parent actor in WorldPartitionActorDesc
            WorldPartitionActorDescSerializeAttachParent,

            // Converted AActor GridPlacement to bIsSpatiallyLoaded flag
            ConvertedActorGridPlacementToSpatiallyLoadedFlag,

            // Fixup for bad default value for GridPlacement_DEPRECATED
            ActorGridPlacementDeprecateDefaultValueFixup,

            // PackedLevelActor started using FWorldPartitionActorDesc (not currently checked against but added as a security)
            PackedLevelActorUseWorldPartitionActorDesc,

            // Add support for actor folder objects
            AddLevelActorFolders,

            // Remove FSkeletalMeshLODModel bulk datas
            RemoveSkeletalMeshLODModelBulkDatas,

            // Exclude brightness from the EncodedHDRCubemap,
            ExcludeBrightnessFromEncodedHDRCubemap,

            // Unified volumetric cloud component quality sample count slider between main and reflection views for consistency
            VolumetricCloudSampleCountUnification,

            // Pose asset GUID generated from source AnimationSequence
            PoseAssetRawDataGUID,

            // Convolution bloom now take into account FPostProcessSettings::BloomIntensity for scatter dispersion.
            ConvolutionBloomIntensity,

            // Serialize FHLODSubActors instead of FGuids in WorldPartition HLODActorDesc
            WorldPartitionHLODActorDescSerializeHLODSubActors,

            // Large Worlds - serialize double types as doubles
            LargeWorldCoordinates,

            // Deserialize old BP float&double types as real numbers for pins
            BlueprintPinsUseRealNumbers,

            // Changed shadow defaults for directional light components, version needed to not affect old things
            UpdatedDirectionalLightShadowDefaults,

            // Refresh geometry collections that had not already generated convex bodies.
            GeometryCollectionConvexDefaults,

            // Add faster damping calculations to the cloth simulation and rename previous Damping parameter to LocalDamping.
            ChaosClothFasterDamping,

            // Serialize LandscapeActorGuid in FLandscapeActorDesc sub class.
            WorldPartitionLandscapeActorDescSerializeLandscapeActorGuid,

            // add inertia tensor and rotation of mass to convex
            AddedInertiaTensorAndRotationOfMassAddedToConvex,

            // Storing inertia tensor as vec3 instead of matrix.
            ChaosInertiaConvertedToVec3,

            // For Blueprint real numbers, ensure that legacy float data is serialized as single-precision
            SerializeFloatPinDefaultValuesAsSinglePrecision,

            // Upgrade the BlendMasks array in existing LayeredBoneBlend nodes
            AnimLayeredBoneBlendMasks,

            // Uses RG11B10 format to store the encoded reflection capture data on mobile
            StoreReflectionCaptureEncodedHDRDataInRG11B10Format,

            // Add WithSerializer type trait and implementation for FRawAnimSequenceTrack
            RawAnimSequenceTrackSerializer,

            // Removed font from FEditableTextBoxStyle, and added FTextBlockStyle instead.
            RemoveDuplicatedStyleInfo,

            // Added member reference to linked anim graphs
            LinkedAnimGraphMemberReference,

            // Changed default tangent behavior for new dynamic mesh components
            DynamicMeshComponentsDefaultUseExternalTangents,

            // Added resize methods to media capture
            MediaCaptureNewResizeMethods,

            // Function data stores a map from work to debug operands
            RigVMSaveDebugMapInGraphFunctionData,

            // Changed default Local Exposure Contrast Scale from 1.0 to 0.8
            LocalExposureDefaultChangeFrom1,

            // Serialize bActorIsListedInSceneOutliner in WorldPartitionActorDesc
            WorldPartitionActorDescSerializeActorIsListedInSceneOutliner,

            // Disabled opencolorio display configuration by default
            OpenColorIODisabledDisplayConfigurationDefault,

            // Serialize ExternalDataLayerAsset in WorldPartitionActorDesc
            WorldPartitionExternalDataLayers,

            // Fix Chaos Cloth fictitious angular scale bug that requires existing parameter rescaling.
            ChaosClothFictitiousAngularVelocitySubframeFix,

            // Store physics thread particles data in single precision
            SinglePrecisonParticleDataPT,

            //Orthographic Near and Far Plane Auto-resolve enabled by default
            OrthographicAutoNearFarPlane,

            // -----<new versions can be added above this line>-------------------------------------------------
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        }

        public static readonly FGuid GUID = new(0xD89B5E42, 0x24BD4D46, 0x8412ACA8, 0xDF641779);

        public static Type Get(FArchive Ar)
        {
            var ver = Ar.CustomVer(GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                EGame.GAME_BlackMythWukong => Type.StoreReflectionCaptureEncodedHDRDataInRG11B10Format,

                < EGame.GAME_UE5_0 => Type.BeforeCustomVersionWasAdded,
                < EGame.GAME_UE5_1 => Type.SerializeFloatPinDefaultValuesAsSinglePrecision,
                < EGame.GAME_UE5_3 => Type.LinkedAnimGraphMemberReference,
                < EGame.GAME_UE5_4 => Type.OpenColorIODisabledDisplayConfigurationDefault,
                _ => Type.LatestVersion
            };
        }
    }
}
