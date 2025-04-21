using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions;

public static class FStateTreeInstanceStorageCustomVersion
{
    public enum Type
    {
        // Before any version changes were made in the plugin
        BeforeCustomVersionWasAdded = 0,
        // Added custom serialization
        AddedCustomSerialization,

        // -----<new versions can be added above this line>-----
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1
    }

    public static readonly FGuid GUID = new(0x60C4F0DE, 0x8B264C34, 0xAA937201, 0x5DFF09CC);

    public static Type Get(FArchive Ar)
    {
        var ver = Ar.CustomVer(GUID);
        if (ver >= 0)
            return (Type) ver;

        return Ar.Game switch
        {
            < EGame.GAME_UE5_4 => Type.BeforeCustomVersionWasAdded,
            _ => Type.LatestVersion
        };
    }
}
