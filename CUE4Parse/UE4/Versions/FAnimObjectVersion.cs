using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions
{
    // Custom serialization version for changes made in Dev-Anim stream
    public static class FAnimObjectVersion
    {
        public enum Type
        {
            // Before any version changes were made
            BeforeCustomVersionWasAdded,

            // Reworked how anim blueprint root nodes are recovered
            LinkTimeAnimBlueprintRootDiscovery,

            // Cached marker sync names on skeleton for editor
            StoreMarkerNamesOnSkeleton,

            // Serialized register array state for RigVM
            SerializeRigVMRegisterArrayState,

            // Increase number of bones per chunk from uint8 to uint16
            IncreaseBoneIndexLimitPerChunk,

            UnlimitedBoneInfluences,

            // Anim sequences have colors for their curves
            AnimSequenceCurveColors,

            // Notifies and sync markers now have Guids
            NotifyAndSyncMarkerGuids,

            // Serialized register dynamic state for RigVM
            SerializeRigVMRegisterDynamicState,

            // Groom cards serialization
            SerializeGroomCards,

            // Serialized rigvm entry names
            SerializeRigVMEntries,

            // Serialized rigvm entry names
            SerializeHairBindingAsset,

            // Serialized rigvm entry names
            SerializeHairClusterCullingData,

            // Groom cards and meshes serialization
            SerializeGroomCardsAndMeshes,

            // Stripping LOD data from groom
            GroomLODStripping,

            // Stripping LOD data from groom
            GroomBindingSerialization,

            // -----<new versions can be added above this line>-------------------------------------------------
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1,
        }

        public static readonly FGuid GUID = new(0xAF43A65D, 0x7FD34947, 0x98733E8E, 0xD9C1BB05);

        public static Type Get(FArchive Ar)
        {
            var ver = Ar.CustomVer(GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                < EGame.GAME_UE4_21 => Type.BeforeCustomVersionWasAdded,
                < EGame.GAME_UE4_25 => Type.StoreMarkerNamesOnSkeleton,
                < EGame.GAME_UE4_26 => Type.NotifyAndSyncMarkerGuids,
                _ => Type.LatestVersion
            };
        }
    }
}