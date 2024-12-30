using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions
{
    // Custom serialization version for changes made in //UE5/Private-Frosty stream
    public static class FUE5PrivateFrostyStreamObjectVersion
    {
        public enum Type
        {
            // Before any version changes were made
            BeforeCustomVersionWasAdded = 0,

            // Added HLODBatchingPolicy member to UPrimitiveComponent, which replaces the confusing bUseMaxLODAsImposter & bBatchImpostersAsInstances.
            HLODBatchingPolicy,

            // Serialize scene components static bounds
            SerializeSceneComponentStaticBounds,

            // Add the long range attachment tethers to the cloth asset to avoid a large hitch during the cloth's initialization.
            ChaosClothAddTethersToCachedData,

            // Always serialize the actor label in cooked builds
            SerializeActorLabelInCookedBuilds,

            // Changed world partition HLODs cells from FSotObjectPath to FName
            ConvertWorldPartitionHLODsCellsToName,

            // Re-calculate the long range attachment to prevent kinematic tethers.
            ChaosClothRemoveKinematicTethers,

            // Serializes the Morph Target render data for cooked platforms and the DDC
            SerializeSkeletalMeshMorphTargetRenderData,

            // Strip the Morph Target source data for cooked builds
            StripMorphTargetSourceDataForCookedBuilds,

            // -----<new versions can be added above this line>-------------------------------------------------
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        };

        public static readonly FGuid GUID = new(0x59DA5D52, 0x12324948, 0xB8785978, 0x70B8E98B);

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
