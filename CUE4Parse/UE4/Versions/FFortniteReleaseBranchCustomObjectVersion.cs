using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions
{
    public class FFortniteReleaseBranchCustomObjectVersion
    {
        public enum Type
        {
            // Before any version changes were made
            BeforeCustomVersionWasAdded = 0,

            // Custom 14.10 File Object Version
            DisableLevelset_v14_10 ,

            // Add the long range attachment tethers to the cloth asset to avoid a large hitch during the cloth's initialization.
            ChaosClothAddTethersToCachedData,

            // Chaos::TKinematicTarget no longer stores a full transform, only position/rotation.
            ChaosKinematicTargetRemoveScale,

            // -----<new versions can be added above this line>-------------------------------------------------
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        }

        public static readonly FGuid GUID = new(0xE7086368, 0x6B234C58, 0x84391B70, 0x16265E91);

        public static Type Get(FArchive Ar)
        {
            var ver = VersionUtils.GetUE4CustomVersion(Ar, GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                < EGame.GAME_UE4_25 => Type.BeforeCustomVersionWasAdded,
                < EGame.GAME_UE5_0 => Type.DisableLevelset_v14_10,
                _ => Type.LatestVersion
            };
        }
    }
}
