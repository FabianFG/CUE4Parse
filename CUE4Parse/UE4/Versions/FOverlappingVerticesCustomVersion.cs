using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Versions
{
    public class FOverlappingVerticesCustomVersion
    {
        public enum Type
        {
            BeforeCustomVersionWasAdded = 0,
            // UE4.19
            DetectOVerlappingVertices = 1,
        };
        
        public static readonly FGuid GUID = new FGuid(0x612FBE52, 0xDA53400B, 0x910D4F91, 0x9FB1857C);
        
        public static Type Get(FAssetArchive Ar)
        {
            int ver = VersionUtils.GetUE4CustomVersion(Ar, GUID);
            if (ver >= 0)
                return (Type)ver;

            if (Ar.Game < EGame.GAME_UE4_19)
                return Type.BeforeCustomVersionWasAdded;
            return Type.DetectOVerlappingVertices;
        }
    }
}