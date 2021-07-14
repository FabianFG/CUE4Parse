using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Versions
{
    public class FSequencerObjectVersion
    {
        public enum Type
        {
            // Before any version changes were made
            BeforeCustomVersionWasAdded = 0,

            // Per-platform overrides player overrides for media sources changed name and type.
            RenameMediaSourcePlatformPlayers,

            // Enable root motion isn't the right flag to use, but force root lock
            ConvertEnableRootMotionToForceRootLock,

            // Convert multiple rows to tracks
            ConvertMultipleRowsToTracks,

            // When finished now defaults to restore state
            WhenFinishedDefaultsToRestoreState,

            // EvaluationTree added
            EvaluationTree,

            // When finished now defaults to project default
            WhenFinishedDefaultsToProjectDefault,

            // When finished now defaults to project default
            FloatToIntConversion,

            // Purged old spawnable blueprint classes from level sequence assets
            PurgeSpawnableBlueprints,

            // Finish UMG evaluation on end
            FinishUMGEvaluation,

            // Manual serialization of float channel
            SerializeFloatChannel,

            // Change the linear keys so they act the old way and interpolate always.
            ModifyLinearKeysForOldInterp,

            // Full Manual serialization of float channel
            SerializeFloatChannelCompletely,

            // -----<new versions can be added above this line>-------------------------------------------------
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        }
        
        public static readonly FGuid GUID = new(0x7B5AE74C, 0xD2704C10, 0xA9585798, 0x0B212A5A);
        
        // TODO: Complete this
        public static Type Get(FAssetArchive Ar)
        {
            var ver = VersionUtils.GetUE4CustomVersion(Ar, GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                _ => Type.LatestVersion
            };
        }
    }
}