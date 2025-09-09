using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions;

// Custom serialization version for changes to variant manager objects
public static class FInterchangeCustomVersion
{
    public enum Type
    {
        // Roughly corresponds to 5.2
        BeforeCustomVersionWasAdded = 0,

        SerializedInterchangeObjectStoring,
        
        MultipleAllocationsPerAttributeInStorage,
        
        // The change that implemented the previous version had to be backed out to fix a serialization issue
        MultipleAllocationsPerAttributeInStorageFixed,
            
        // -----<new versions can be added above this line>-------------------------------------------------
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1
    };

    public static readonly FGuid GUID = new FGuid(0x92738C43, 0x29884D9C, 0x9A3D9BBE, 0x6EFF9FC0);

    public static Type Get(FArchive Ar)
    {
        var ver = Ar.CustomVer(GUID);
        if (ver >= 0)
            return (Type) ver;

        return Ar.Game switch
        {
            < EGame.GAME_UE5_2 => Type.BeforeCustomVersionWasAdded,
            < EGame.GAME_UE5_7 => Type.SerializedInterchangeObjectStoring,
            _ => Type.LatestVersion
        };
    }
}