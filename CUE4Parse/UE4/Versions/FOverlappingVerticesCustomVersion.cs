using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions
{
    // custom version for overlapping vertcies code
    public static class FOverlappingVerticesCustomVersion
    {
        public enum Type
        {
            // Before any version changes were made in the plugin
            BeforeCustomVersionWasAdded = 0,
            // UE4.19
            // Converted to use HierarchicalInstancedStaticMeshComponent
            DetectOVerlappingVertices = 1,
            // -----<new versions can be added above this line>-------------------------------------------------
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        }

        public static readonly FGuid GUID = new(0x612FBE52, 0xDA53400B, 0x910D4F91, 0x9FB1857C);

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