using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions;

// Custom serialization version for all packages containing DNAAsset / DNA types
public class FDNAAssetCustomVersion
{
    public enum Type
    {
        // Before any version changes were made in the plugin
        BeforeCustomVersionWasAdded = 0,
        UnifiedReaderVersion = 1,
        IntroduceOptimizedSerializationDuringCooking = 2,
        GeneralizedCoordinateSystemConversion = 3,
        UserModifiableDNAConfig = 4,

        // -----<new versions can be added above this line>-------------------------------------------------
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1
    }

    public static readonly FGuid GUID = new(0x9DE7BD98, 0x67D445B2, 0x8C0E9D73, 0xFDE1E367);

    public static Type Get(FArchive Ar)
    {
        var ver = Ar.CustomVer(GUID);
        if (ver >= 0)
            return (Type) ver;

        return Ar.Game switch
        {
            < GAME_UE4_26 => (Type) (-1),
            _ => Type.LatestVersion
        };
    }
}
