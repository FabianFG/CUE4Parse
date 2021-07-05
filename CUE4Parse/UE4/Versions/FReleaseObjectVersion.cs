using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Versions
{
    class FReleaseObjectVersion
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
            int ver = VersionUtils.GetUE4CustomVersion(Ar, GUID);
            if (ver >= 0)
                return (Type)ver;
            if (Ar.Game < EGame.GAME_UE4_11)
                return Type.BeforeCustomVersionWasAdded;
            if (Ar.Game < EGame.GAME_UE4_13)
                return (Type)1;
            if (Ar.Game < EGame.GAME_UE4_14)
                return (Type)3;
            if (Ar.Game < EGame.GAME_UE4_15)
                return (Type)4;
            if (Ar.Game < EGame.GAME_UE4_16)
                return (Type)7;
            if (Ar.Game < EGame.GAME_UE4_17)
                return (Type)9;
            if (Ar.Game < EGame.GAME_UE4_19)
                return (Type)10;
            if (Ar.Game < EGame.GAME_UE4_20)
                return Type.AddSkeletalMeshSectionDisable;
            if (Ar.Game < EGame.GAME_UE4_21)
                return (Type)17;
            if (Ar.Game < EGame.GAME_UE4_23)
                return (Type)20;
            if (Ar.Game < EGame.GAME_UE4_24)
                return (Type)23;
            if (Ar.Game < EGame.GAME_UE4_25)
                return (Type)28;
            if (Ar.Game < EGame.GAME_UE4_26)
                return (Type)30;
//          if (Ar.Game < EGame.GAME_UE4_27)
                return (Type)37;
            // NEW_ENGINE_VERSION
//          return LatestVersion;
        }
    }
}