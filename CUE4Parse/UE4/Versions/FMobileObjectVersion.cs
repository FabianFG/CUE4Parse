using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions
{
    // Custom serialization version for changes made in Dev-Mobile stream
    public static class FMobileObjectVersion
    {
        public enum Type
        {
            // Before any version changes were made
            BeforeCustomVersionWasAdded = 0,

            // Removed LightmapUVBias, ShadowmapUVBias from per-instance data
            InstancedStaticMeshLightmapSerialization,

            // Added stationary point/spot light direct contribution to volumetric lightmaps.
            LQVolumetricLightmapLayers,

            // Store Reflection Capture in compressed format for mobile
            StoreReflectionCaptureCompressedMobile,

            // -----<new versions can be added above this line>-------------------------------------------------
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        }

        public static readonly FGuid GUID = new(0xCFFC743F, 0x43B04480, 0x939114DF, 0x171D2073);

        public static Type Get(FArchive Ar)
        {
            var ver = Ar.CustomVer(GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                < EGame.GAME_UE4_19 => Type.BeforeCustomVersionWasAdded,
                < EGame.GAME_UE4_26 => Type.LQVolumetricLightmapLayers,
                _ => Type.LatestVersion
            };
        }
    }
}
