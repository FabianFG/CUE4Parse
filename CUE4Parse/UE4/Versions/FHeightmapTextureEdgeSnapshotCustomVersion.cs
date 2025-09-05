using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions;

// Custom serialization version for changes to HeightmapTextureEdgeSnapshot
public static class FHeightmapTextureEdgeSnapshotCustomVersion
{
    public enum Type
    {
        BeforeCustomVersionWasAdded = 0,
        BeforeInitialHashWasAdded = 1,
        BeforeCornerDataWasRemoved = 2,
        BeforeChangedCornerHash = 3,
        BeforeChangedCookedFormat = 4,
        LatestVersion = 5
    }

    public static readonly FGuid GUID = new(0x12345678, 0x12345678, 0x12345678, 0x12345678);

    public static Type Get(FArchive Ar)
    {
        var ver = Ar.CustomVer(GUID);
        if (ver >= 0)
            return (Type) ver;

        return Ar.Game switch
        {
            < EGame.GAME_UE5_6 => Type.BeforeCustomVersionWasAdded,
            _ => Type.LatestVersion
        };
    }
}
