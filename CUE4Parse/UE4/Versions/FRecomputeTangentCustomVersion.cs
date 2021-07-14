using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Versions
{
    public static class FRecomputeTangentCustomVersion
    {
        public enum Type
        {
            BeforeCustomVersionWasAdded = 0,

            // UE4.12
            RuntimeRecomputeTangent = 1,

            // UE4.26
            RecomputeTangentVertexColorMask = 2,

            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        };

        public static readonly FGuid GUID = new(0x5579F886, 0x933A4C1F, 0x83BA087B, 0x6361B92F);

        public static Type Get(FAssetArchive Ar)
        {
            var ver = VersionUtils.GetUE4CustomVersion(Ar, GUID);
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
}