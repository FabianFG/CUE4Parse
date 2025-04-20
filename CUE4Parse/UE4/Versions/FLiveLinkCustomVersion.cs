using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions;

public static class FLiveLinkCustomVersion
{
    public enum Type
    {
        // Before any version changes were made in the plugin
        BeforeCustomVersionWasAdded = 0,

        NewLiveLinkRoleSystem,

        // -----<new versions can be added above this line>-----
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1
    }

    public static readonly FGuid GUID = new(0xab965196, 0x45d808fc, 0xb7d7228d, 0x78ad569e);

    public static Type Get(FArchive Ar)
    {
        var ver = Ar.CustomVer(GUID);
        if (ver >= 0)
            return (Type) ver;

        return Ar.Game switch
        {
            < EGame.GAME_UE4_23 => Type.BeforeCustomVersionWasAdded,
            _ => Type.LatestVersion
        };
    }
}
