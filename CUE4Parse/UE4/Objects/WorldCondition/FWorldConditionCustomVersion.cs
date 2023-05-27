using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.WorldCondition;

public static class FWorldConditionCustomVersion
{
    public enum Type
    {
        // Before any version changes were made in the plugin
        BeforeCustomVersionWasAdded = 0,
        // Changed shared definition to a struct.
        StructSharedDefinition = 1,
        // -----<new versions can be added above this line>-------------------------------------------------
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1
    }

    public static readonly FGuid GUID = new(0x2C28AC22, 0x15CF46FE, 0xBD19F011, 0x652A3C05);

    public static Type Get(FArchive Ar)
    {
        var ver = Ar.CustomVer(GUID);
        if (ver >= 0)
            return (Type) ver;

        return Ar.Game switch
        {
            < EGame.GAME_UE5_1 => Type.BeforeCustomVersionWasAdded,
            _ => Type.StructSharedDefinition
        };
    }
}
