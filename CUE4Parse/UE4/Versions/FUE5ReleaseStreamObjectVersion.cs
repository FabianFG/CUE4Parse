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
                < EGame.GAME_UE5_0 => Type.BeforeCustomVersionWasAdded,
                _ => Type.LatestVersion // TODO change this after they released UE5.0
            };
        }
    }
}