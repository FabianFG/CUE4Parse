using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions;

// Custom serialization version for changes to variant manager objects
public static class FVariantManagerObjectVersion
{
    public enum Type
    {
        // Roughly corresponds to 4.21
        BeforeCustomVersionWasAdded = 0,

        CorrectSerializationOfFNameBytes,

        CategoryFlagsAndManualDisplayText,

        CorrectSerializationOfFStringBytes,

        SerializePropertiesAsNames,

        StoreDisplayOrder,

        // -----<new versions can be added above this line>-------------------------------------------------
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1
    }

    public static readonly FGuid GUID = new(0x24BB7AF3, 0x56464F83, 0x1F2F2DC2, 0x49AD96FF);

    public static Type Get(FArchive Ar)
    {
        var ver = Ar.CustomVer(GUID);
        if (ver >= 0)
            return (Type) ver;

        return Ar.Game switch
        {
            < EGame.GAME_UE4_22 => Type.BeforeCustomVersionWasAdded,
            < EGame.GAME_UE4_23 => Type.SerializePropertiesAsNames,
            _ => Type.LatestVersion
        };
    }
}
