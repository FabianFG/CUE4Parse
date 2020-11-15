using System.Runtime.CompilerServices;
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
        };

        public static readonly FGuid GUID = new FGuid(0xAF43A65D, 0x7FD34947, 0x98733E8E, 0xD9C1BB05);
        
        public static Type Get(FAssetArchive Ar)
        {

            int ver = VersionUtils.GetUE4CustomVersion(Ar, GUID);
            if (ver >= 0)
                return (Type)ver;

            if (Ar.Game < EGame.GAME_UE4_21)
                return Type.BeforeCustomVersionWasAdded;
            if (Ar.Game < EGame.GAME_UE4_25)
                return Type.StoreMarkerNamesOnSkeleton;
            if (Ar.Game < EGame.GAME_UE4_26)
                return (Type)7;
            if (Ar.Game < EGame.GAME_UE4_27)
                return (Type) 15;

            return Type.LatestVersion;
        }
    }
}