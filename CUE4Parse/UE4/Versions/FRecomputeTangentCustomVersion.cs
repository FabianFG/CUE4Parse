using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions;

// Custom serialization version for RecomputeTangent
public static class FRecomputeTangentCustomVersion
{
    public enum Type
    {
        // Before any version changes were made in the plugin
        BeforeCustomVersionWasAdded = 0,
        // UE4.12
        // We serialize the RecomputeTangent Option
        RuntimeRecomputeTangent = 1,
        // UE4.26
        // Choose which Vertex Color channel to use as mask to blend tangents
        RecomputeTangentVertexColorMask = 2,
        // -----<new versions can be added above this line>-------------------------------------------------
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1
    }

    public static readonly FGuid GUID = new(0x5579F886, 0x933A4C1F, 0x83BA087B, 0x6361B92F);

    public static Type Get(FArchive Ar)
    {
        var ver = Ar.CustomVer(GUID);
        if (ver >= 0)
            return (Type) ver;

        return Ar.Game switch
        {
            < EGame.GAME_UE4_12 => Type.BeforeCustomVersionWasAdded,
            < EGame.GAME_UE4_26 => Type.RuntimeRecomputeTangent,
            _ => Type.RecomputeTangentVertexColorMask
        };
    }
}
