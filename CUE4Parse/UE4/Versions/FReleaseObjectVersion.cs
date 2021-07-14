using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Versions
{
    public static class FReleaseObjectVersion
    {
        public enum Type
        {
            BeforeCustomVersionWasAdded = 0,

            // UE4.19 = 12
            AddSkeletalMeshSectionDisable = 12,
            PropertiesSerializeRepCondition = 21,

            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        }

        public static readonly FGuid GUID = new(0x9C54D522, 0xA8264FBE, 0x94210746, 0x61B482D0);

        public static Type Get(FAssetArchive Ar)
        {
            var ver = VersionUtils.GetUE4CustomVersion(Ar, GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                < EGame.GAME_UE4_11 => Type.BeforeCustomVersionWasAdded,
                < EGame.GAME_UE4_13 => (Type) 1,
                < EGame.GAME_UE4_14 => (Type) 3,
                < EGame.GAME_UE4_15 => (Type) 4,
                < EGame.GAME_UE4_16 => (Type) 7,
                < EGame.GAME_UE4_17 => (Type) 9,
                < EGame.GAME_UE4_19 => (Type) 10,
                < EGame.GAME_UE4_20 => Type.AddSkeletalMeshSectionDisable,
                < EGame.GAME_UE4_21 => (Type) 17,
                < EGame.GAME_UE4_23 => (Type) 20,
                < EGame.GAME_UE4_24 => (Type) 23,
                < EGame.GAME_UE4_25 => (Type) 28,
                < EGame.GAME_UE4_26 => (Type) 30,
                _ => (Type) 37
            };
        }
    }
}