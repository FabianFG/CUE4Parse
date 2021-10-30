using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions
{
    // Custom serialization version for changes made for a private stream
    public static class FReflectionCaptureObjectVersion
    {
        public enum Type
        {
            // Before any version changes were made
            BeforeCustomVersionWasAdded = 0,

            // Allows uncompressed reflection captures for cooked builds
            MoveReflectionCaptureDataToMapBuildData,

            // -----<new versions can be added above this line>-------------------------------------------------
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        }

        public static readonly FGuid GUID = new(0x6B266CEC, 0x1EC74B8F, 0xA30BE4D9, 0x0942FC07);

        public static Type Get(FArchive Ar)
        {
            var ver = Ar.CustomVer(GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                < EGame.GAME_UE4_19 => Type.BeforeCustomVersionWasAdded,
                _ => Type.LatestVersion
            };
        }
    }
}
