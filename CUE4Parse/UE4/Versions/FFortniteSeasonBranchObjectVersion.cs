using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions
{
    public static class FFortniteSeasonBranchObjectVersion
    {
        public enum Type
        {
            // Before any version changes were made
            BeforeCustomVersionWasAdded = 0,

            // Added FWorldDataLayersActorDesc
            AddedWorldDataLayersActorDesc,

            // Fixed FDataLayerInstanceDesc
            FixedDataLayerInstanceDesc,

            // Serialize DataLayerAssets in WorldPartitionActorDesc
            WorldPartitionActorDescSerializeDataLayerAssets,

            // Remapped bEvaluateWorldPositionOffset to bEvaluateWorldPositionOffsetInRayTracing
            RemappedEvaluateWorldPositionOffsetInRayTracing,

            // Serialize native and base class for actor descriptors
            WorldPartitionActorDescNativeBaseClassSerialization,

            // Serialize tags for actor descriptors
            WorldPartitionActorDescTagsSerialization,

            // Serialize property map for actor descriptors
            WorldPartitionActorDescPropertyMapSerialization,

            // Added ability to mark shapes as probes
            AddShapeIsProbe,

            // Transfer PhysicsAsset SolverSettings (iteration counts etc) to new structure
            PhysicsAssetNewSolverSettings,

            // Chaos GeometryCollection now saves levels attribute values
            ChaosGeometryCollectionSaveLevelsAttribute,

            // Serialize actor transform for actor descriptors
            WorldPartitionActorDescActorTransformSerialization,

            // Changing Chaos::FImplicitObjectUnion to store an int32 vs a uint16 for NumLeafObjects.
            ChaosImplicitObjectUnionLeafObjectsToInt32,

            // Chaos Visual Debugger : Adding serialization for properties that were being recorded, but not serialized
            CVDSerializationFixMissingSerializationProperties,

            // -----<new versions can be added above this line>-------------------------------------------------
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        }

        public static readonly FGuid GUID = new(0x5B4C06B7, 0x24634AF8, 0x805BBF70, 0xCDF5D0DD);

        public static Type Get(FArchive Ar)
        {
            var ver = Ar.CustomVer(GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                < EGame.GAME_UE5_1 => Type.BeforeCustomVersionWasAdded,
                < EGame.GAME_UE5_4 => Type.ChaosGeometryCollectionSaveLevelsAttribute,
                < EGame.GAME_UE5_5 => Type.ChaosImplicitObjectUnionLeafObjectsToInt32,
                _ => Type.LatestVersion
            };
        }
    }
}
