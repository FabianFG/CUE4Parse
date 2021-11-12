using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions
{
    // Custom serialization version for changes made in Dev-Core stream
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
        }

        public static readonly FGuid GUID = new(0x375EC13C, 0x06E448FB, 0xB50084F0, 0x262A717E);

        public static Type Get(FArchive Ar)
        {
            var ver = Ar.CustomVer(GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                < EGame.GAME_UE4_12 => Type.BeforeCustomVersionWasAdded,
                < EGame.GAME_UE4_15 => Type.MaterialInputNativeSerialize,
                < EGame.GAME_UE4_22 => Type.EnumProperties,
                < EGame.GAME_UE4_25 => Type.SkeletalMaterialEditorDataStripping,
                _ => Type.FProperties
            };
        }
    }
}