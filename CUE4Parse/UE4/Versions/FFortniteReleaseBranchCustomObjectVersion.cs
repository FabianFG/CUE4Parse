using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions;

// Custom serialization version for changes made in the //Fortnite/Release-XX.XX stream
public static class FFortniteReleaseBranchCustomObjectVersion
{
    public enum Type
    {
        // Before any version changes were made
        BeforeCustomVersionWasAdded = 0,

        // Custom 14.10 File Object Version
        DisableLevelset_v14_10,

        // Add the long range attachment tethers to the cloth asset to avoid a large hitch during the cloth's initialization.
        ChaosClothAddTethersToCachedData,

        // Chaos::TKinematicTarget no longer stores a full transform, only position/rotation.
        ChaosKinematicTargetRemoveScale,

        // Move UCSModifiedProperties out of ActorComponent and in to sparse storage
        ActorComponentUCSModifiedPropertiesSparseStorage,

        // Fixup Nanite meshes which were using the wrong material and didn't have proper UVs :
        FixupNaniteLandscapeMeshes,

        // Remove any cooked collision data from nanite landscape / editor spline meshes since collisions are not needed there :
        RemoveUselessLandscapeMeshesCookedCollisionData,

        // Serialize out UAnimCurveCompressionCodec::InstanceGUID to maintain deterministic DDC key generation in cooked-editor
        SerializeAnimCurveCompressionCodecGuidOnCook,

        // Fix the Nanite landscape mesh being reused because of a bad name
        FixNaniteLandscapeMeshNames,

        // Fixup and synchronize shared properties modified before the synchronicity enforcement
        LandscapeSharedPropertiesEnforcement,

        // Include the cell size when computing the cell guid
        WorldPartitionRuntimeCellGuidWithCellSize,

        // Enable SkipOnlyEditorOnly style cooking of NaniteOverrideMaterial
        NaniteMaterialOverrideUsesEditorOnly,

        // Store game thread particles data in single precision
        SinglePrecisionParticleData,

        // UPCGPoint custom serialization
        PCGPointStructuredSerializer,

        // Deprecation of Nav Movement Properties and moving them to a new struct
        NavMovementComponentMovingPropertiesToStruct,

        // Add bone serialization for dynamic mesh attributes
        DynamicMeshAttributesSerializeBones,

        // -----<new versions can be added above this line>-------------------------------------------------
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1
    }

    public static readonly FGuid GUID = new(0xE7086368, 0x6B234C58, 0x84391B70, 0x16265E91);

    public static Type Get(FArchive Ar)
    {
        var ver = Ar.CustomVer(GUID);
        if (ver >= 0)
            return (Type) ver;

        return Ar.Game switch
        {
            < EGame.GAME_UE4_25 => Type.BeforeCustomVersionWasAdded,
            < EGame.GAME_UE5_0 => Type.DisableLevelset_v14_10,
            < EGame.GAME_UE5_1 => Type.ChaosKinematicTargetRemoveScale,
            < EGame.GAME_UE5_2 => Type.ActorComponentUCSModifiedPropertiesSparseStorage,
            < EGame.GAME_UE5_3 => Type.RemoveUselessLandscapeMeshesCookedCollisionData,
            < EGame.GAME_UE5_4 => Type.NaniteMaterialOverrideUsesEditorOnly,
            < EGame.GAME_UE5_5 => Type.PCGPointStructuredSerializer,
            _ => Type.LatestVersion
        };
    }
}
