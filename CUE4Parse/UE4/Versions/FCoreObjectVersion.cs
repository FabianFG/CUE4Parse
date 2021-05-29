using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;

namespace CUE4Parse.UE4.Versions
{
    public static class FCoreObjectVersion
    {
        public enum Type
        {
            // Before any version changes were made
            BeforeCustomVersionWasAdded = 0,
            MaterialInputNativeSerialize,
            EnumProperties,
            SkeletalMaterialEditorDataStripping,
            FProperties,

            // -----<new versions can be added above this line>-------------------------------------------------
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        };
        
        public static readonly FGuid GUID = new FGuid(0x375EC13C, 0x06E448FB, 0xB50084F0, 0x262A717E);
        
        public static Type Get(FAssetArchive Ar)
        {
            int ver = VersionUtils.GetUE4CustomVersion(Ar, GUID);
            if (ver >= 0)
                return (Type)ver;

            if (Ar.Game < EGame.GAME_UE4_12)
                return Type.BeforeCustomVersionWasAdded;
            if (Ar.Game < EGame.GAME_UE4_15)
                return Type.MaterialInputNativeSerialize;
            if (Ar.Game < EGame.GAME_UE4_22)
                return Type.EnumProperties;
            if (Ar.Game < EGame.GAME_UE4_25)
                return Type.SkeletalMaterialEditorDataStripping;
//		if (Ar.Game < GAME_UE4(27))
            return Type.FProperties;
            // NEW_ENGINE_VERSION
//		return LatestVersion;
        }
    }
}