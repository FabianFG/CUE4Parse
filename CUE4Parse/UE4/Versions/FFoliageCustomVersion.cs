using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions;

// Custom serialization version for changes made in Dev-Anim stream
public static class FFoliageCustomVersion
{
    public enum Type
    {
        // Before any version changes were made in the plugin
        BeforeCustomVersionWasAdded = 0,
        // Converted to use HierarchicalInstancedStaticMeshComponent
        FoliageUsingHierarchicalISMC = 1,
        // Changed Component to not RF_Transactional
        HierarchicalISMCNonTransactional = 2,
        // Added FoliageTypeUpdateGuid
        AddedFoliageTypeUpdateGuid = 3,
        // Use a GUID to determine whic procedural actor spawned us
        ProceduralGuid = 4,
        // Support for cross-level bases
        CrossLevelBase = 5,
        // FoliageType for details customization
        FoliageTypeCustomization = 6,
        // FoliageType for details customization continued
        FoliageTypeCustomizationScaling = 7,
        // FoliageType procedural scale and shade settings updated
        FoliageTypeProceduralScaleAndShade = 8,
        // Added FoliageHISMC and blueprint support
        FoliageHISMCBlueprints = 9,
        // Added Mobility setting to UFoliageType
        AddedMobility = 10,
        // Make sure that foliage has FoliageHISMC class
        FoliageUsingFoliageISMC = 11,
        // Foliage Actor Support
        FoliageActorSupport = 12,
        // Foliage Actor (No weak ptr)
        FoliageActorSupportNoWeakPtr = 13,
        // Foliage Instances are now always saved local to Level
        FoliageRepairInstancesWithLevelTransform = 14,
        // Supports discarding foliage types on load independently from density scaling
        FoliageDiscardOnLoad = 15,

        // -----<new versions can be added above this line>-------------------------------------------------
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1,
    }

    public static readonly FGuid GUID = new(0x430C4D19, 0x71544970, 0x87699B69, 0xDF90B0E5);

    public static Type Get(FArchive Ar)
    {
        var ver = Ar.CustomVer(GUID);
        if (ver >= 0)
            return (Type) ver;

        return Ar.Game switch
        {
            < EGame.GAME_UE4_7 => Type.BeforeCustomVersionWasAdded,
            < EGame.GAME_UE4_8 => Type.AddedFoliageTypeUpdateGuid,
            < EGame.GAME_UE4_9 => Type.FoliageTypeProceduralScaleAndShade,
            < EGame.GAME_UE4_10 => Type.AddedMobility,
            < EGame.GAME_UE4_23 => Type.FoliageUsingFoliageISMC,
            < EGame.GAME_UE4_24 => Type.FoliageActorSupportNoWeakPtr,
            < EGame.GAME_UE4_26 => Type.FoliageRepairInstancesWithLevelTransform,
            _ => Type.LatestVersion
        };
    }
}
