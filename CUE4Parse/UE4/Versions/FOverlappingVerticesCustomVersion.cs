using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Versions
{
    public static class FOverlappingVerticesCustomVersion
    {
        public enum Type
        {
            BeforeCustomVersionWasAdded = 0,

            // UE4.19
            DetectOVerlappingVertices = 1,
        }

        public static readonly FGuid GUID = new(0x612FBE52, 0xDA53400B, 0x910D4F91, 0x9FB1857C);

        public static Type Get(FAssetArchive Ar)
        {
            var ver = VersionUtils.GetUE4CustomVersion(Ar, GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                < EGame.GAME_UE4_19 => Type.BeforeCustomVersionWasAdded,
                _ => Type.DetectOVerlappingVertices
            };
        }
    }
}