using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions
{
    public static class FDestructionObjectVersion
    {
        public enum Type
        {
            // Before any version changes were made
            BeforeCustomVersionWasAdded = 0,

            // Added timestamped caches for geometry component to handle transform sampling instead of per-frame
            AddedTimestampedGeometryComponentCache,

            // Added functionality to strip unnecessary data from geometry collection caches
            AddedCacheDataReduction,

            // Geometry collection data is now in the DDC
            GeometryCollectionInDDC,

            // Geometry collection data is now in both the DDC and the asset
            GeometryCollectionInDDCAndAsset,

            // New way to serialize unique ptr and serializable ptr
            ChaosArchiveAdded,

            // Serialization support for UFieldSystems
            FieldsAdded,

            // density default units changed from kg/cm3 to kg/m3
            DensityUnitsChanged,

            // bulk serialize arrays
            BulkSerializeArrays,

            // bulk serialize arrays
            GroupAndAttributeNameRemapping,

            // bulk serialize arrays
            ImplicitObjectDoCollideAttribute,


            // -----<new versions can be added above this line>-------------------------------------------------
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        }

        public static readonly FGuid GUID = new(0x174F1F0B, 0xB4C645A5, 0xB13F2EE8, 0xD0FB917D);

        public static Type Get(FArchive Ar)
        {
            var ver = Ar.CustomVer(GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                < EGame.GAME_UE4_22 => Type.BeforeCustomVersionWasAdded,
                < EGame.GAME_UE4_23 => Type.AddedCacheDataReduction,
                < EGame.GAME_UE4_25 => Type.GroupAndAttributeNameRemapping,
                _ => Type.LatestVersion
            };
        }
    }
}
