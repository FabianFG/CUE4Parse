using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions;

// Custom serialization version for changes made in Dev-Anim stream
public static class FPropertyBagCustomVersion
{
    public enum Type
    {
        // Before any version changes were made in the plugin
        BeforeCustomVersionWasAdded = 0,

        // Added support for array types
        ContainerTypes = 1,
        NestedContainerTypes = 2,
        MetaClass = 3,

        // -----<new versions can be added above this line>-------------------------------------------------
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1,
    }

    public static readonly FGuid GUID = new(0x134A157E, 0xD5E249A3, 0x8D4E843C, 0x98FE9E31);

    public static Type Get(FArchive Ar)
    {
        var ver = Ar.CustomVer(GUID);
        if (ver >= 0)
            return (Type) ver;

        return Ar.Game switch
        {
            EGame.GAME_BlackMythWukong => Type.NestedContainerTypes,
            < EGame.GAME_UE5_1 => Type.BeforeCustomVersionWasAdded,
            < EGame.GAME_UE5_3 => Type.ContainerTypes,
            < EGame.GAME_UE5_4 => Type.NestedContainerTypes,
            _ => Type.LatestVersion
        };
    }
}
