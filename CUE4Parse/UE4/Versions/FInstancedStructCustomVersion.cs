using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions;

public static class FInstancedStructCustomVersion
{
    public enum Type
    {
        // Before any version changes were made
        CustomVersionAdded = 0,

        // -----<new versions can be added above this line>-----
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1
    }
    
    public static readonly FGuid GUID = new(0xE21E1CAA, 0xAF47425E, 0x89BF6AD4, 0x4C44A8BB);
    
    public static Type Get(FArchive Ar)
    {
        var ver = Ar.CustomVer(GUID);
        if (ver >= 0)
            return (Type) ver;

        return Ar.Game switch
        {
            < EGame.GAME_UE5_3 => (Type) (-1),
            _ => Type.LatestVersion
        };
    }
}