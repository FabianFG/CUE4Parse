using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Versions
{
    public static class FAnimObjectVersion
    {
        public enum Type
        {
            BeforeCustomVersionWasAdded,
            LinkTimeAnimBlueprintRootDiscovery,
            StoreMarkerNamesOnSkeleton,
            SerializeRigVMRegisterArrayState,
            IncreaseBoneIndexLimitPerChunk,
            UnlimitedBoneInfluences,
            AnimSequenceCurveColors,
            NotifyAndSyncMarkerGuids,
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1,
        }

        public static readonly FGuid GUID = new(0xAF43A65D, 0x7FD34947, 0x98733E8E, 0xD9C1BB05);

        public static Type Get(FAssetArchive Ar)
        {
            var ver = VersionUtils.GetUE4CustomVersion(Ar, GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                < EGame.GAME_UE4_21 => Type.BeforeCustomVersionWasAdded,
                < EGame.GAME_UE4_25 => Type.StoreMarkerNamesOnSkeleton,
                < EGame.GAME_UE4_26 => (Type) 7,
                < EGame.GAME_UE4_27 => (Type) 15,
                _ => Type.LatestVersion
            };
        }
    }
}