using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions
{
    // Custom serialization version for changes made in Dev-Sequencer stream
    public static class FSequencerObjectVersion
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

            // Set ContinuouslyRespawn to false by default, added FMovieSceneSpawnable::bNetAddressableName
            SpawnableImprovements,

            // -----<new versions can be added above this line>-------------------------------------------------
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        }

        public static readonly FGuid GUID = new(0x7B5AE74C, 0xD2704C10, 0xA9585798, 0x0B212A5A);

        public static Type Get(FArchive Ar)
        {
            var ver = Ar.CustomVer(GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                < EGame.GAME_UE4_14 => Type.BeforeCustomVersionWasAdded,
                < EGame.GAME_UE4_15 => Type.RenameMediaSourcePlatformPlayers,
                < EGame.GAME_UE4_16 => Type.ConvertMultipleRowsToTracks,
                < EGame.GAME_UE4_19 => Type.WhenFinishedDefaultsToRestoreState,
                < EGame.GAME_UE4_20 => Type.WhenFinishedDefaultsToProjectDefault,
                < EGame.GAME_UE4_22 => Type.FinishUMGEvaluation,
                < EGame.GAME_UE4_25 => Type.ModifyLinearKeysForOldInterp,
                < EGame.GAME_UE4_27 => Type.SerializeFloatChannelCompletely,
                _ => Type.LatestVersion
            };
        }
    }
}