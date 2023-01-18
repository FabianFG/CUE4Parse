using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Versions
{
    // Custom serialization version for changes made in Dev-Rendering stream
    public static class FRenderingObjectVersion
    {
        public enum Type
        {
            // Before any version changes were made
            BeforeCustomVersionWasAdded = 0,

            // Added support for 3 band SH in the ILC
            IndirectLightingCache3BandSupport,

            // Allows specifying resolution for reflection capture probes
            CustomReflectionCaptureResolutionSupport,

            RemovedTextureStreamingLevelData,

            // translucency is now a property which matters for materials with the decal domain
            IntroducedMeshDecals,

            // Reflection captures are no longer prenormalized
            ReflectionCapturesStoreAverageBrightness,

            ChangedPlanarReflectionFadeDefaults,

            RemovedRenderTargetSize,

            // Particle Cutout (SubUVAnimation) data is now stored in the ParticleRequired Module
            MovedParticleCutoutsToRequiredModule,

            MapBuildDataSeparatePackage,

            // StaticMesh and SkeletalMesh texcoord size data.
            TextureStreamingMeshUVChannelData,

            // Added type handling to material normalize and length (sqrt) nodes
            TypeHandlingForMaterialSqrtNodes,

            FixedBSPLightmaps,

            DistanceFieldSelfShadowBias,

            FixedLegacyMaterialAttributeNodeTypes,

            ShaderResourceCodeSharing,

            MotionBlurAndTAASupportInSceneCapture2d,

            AddedTextureRenderTargetFormats,

            // Triggers a rebuild of the mesh UV density while also adding an update in the postedit
            FixedMeshUVDensity,

            AddedbUseShowOnlyList,

            VolumetricLightmaps,

            MaterialAttributeLayerParameters,

            StoreReflectionCaptureBrightnessForCooking,

            // FModelVertexBuffer does serialize a regular TArray instead of a TResourceArray
            ModelVertexBufferSerialization,

            ReplaceLightAsIfStatic,

            // Added per FShaderType permutation id.
            ShaderPermutationId,

            // Changed normal precision in imported data
            IncreaseNormalPrecision,

            VirtualTexturedLightmaps,

            GeometryCacheFastDecoder,

            LightmapHasShadowmapData,

            // Removed old gaussian and bokeh DOF methods from deferred shading renderer.
            DiaphragmDOFOnlyForDeferredShadingRenderer,

            // Lightmaps replace ULightMapVirtualTexture (non-UTexture derived class) with ULightMapVirtualTexture2D (derived from UTexture)
            VirtualTexturedLightmapsV2,

            SkyAtmosphereStaticLightingVersioning,

            // UTextureRenderTarget2D now explicitly allows users to create sRGB or non-sRGB type targets
            ExplicitSRGBSetting,

            VolumetricLightmapStreaming,

            //ShaderModel4 support removed from engine
            RemovedSM4,

            // Deterministic ShaderMapID serialization
            MaterialShaderMapIdSerialization,

            // Add force opaque flag for static mesh
            StaticMeshSectionForceOpaqueField,

            // Add force opaque flag for static mesh
            AutoExposureChanges,

            // Removed emulated instancing from instanced static meshes
            RemovedEmulatedInstancing,

            // Added per instance custom data (for Instanced Static Meshes)
            PerInstanceCustomData,

            // Added material attributes to shader graph to support anisotropic materials
            AnisotropicMaterial,

            // Add if anything has changed in the exposure, override the bias to avoid the new default propagating
            AutoExposureForceOverrideBiasFlag,

            // Override for a special case for objects that were serialized and deserialized between versions AutoExposureChanges and AutoExposureForceOverrideBiasFlag
            AutoExposureDefaultFix,

            // Remap Volume Extinction material input to RGB
            VolumeExtinctionBecomesRGB,

            // Add a new virtual texture to support virtual texture light map on mobile
            VirtualTexturedLightmapsV3,

            // -----<new versions can be added above this line>-------------------------------------------------
            VersionPlusOne,
            LatestVersion = VersionPlusOne - 1
        }

        public static readonly FGuid GUID = new(0x12F88B9F, 0x88754AFC, 0xA67CD90C, 0x383ABD29);

        public static Type Get(FArchive Ar)
        {
            var ver = Ar.CustomVer(GUID);
            if (ver >= 0)
                return (Type) ver;

            return Ar.Game switch
            {
                // Game Overrides
                EGame.GAME_TEKKEN7 => Type.MapBuildDataSeparatePackage,

                // Engine
                < EGame.GAME_UE4_12 => Type.BeforeCustomVersionWasAdded,
                < EGame.GAME_UE4_13 => Type.CustomReflectionCaptureResolutionSupport,
                < EGame.GAME_UE4_14 => Type.IntroducedMeshDecals,
                < EGame.GAME_UE4_16 => Type.FixedBSPLightmaps, // 4.14 and 4.15
                < EGame.GAME_UE4_17 => Type.ShaderResourceCodeSharing,
                < EGame.GAME_UE4_18 => Type.AddedbUseShowOnlyList,
                < EGame.GAME_UE4_19 => Type.VolumetricLightmaps,
                < EGame.GAME_UE4_20 => Type.ShaderPermutationId,
                < EGame.GAME_UE4_21 => Type.IncreaseNormalPrecision,
                < EGame.GAME_UE4_22 => Type.VirtualTexturedLightmaps,
                < EGame.GAME_UE4_23 => Type.GeometryCacheFastDecoder,
                < EGame.GAME_UE4_24 => Type.VirtualTexturedLightmapsV2,
                < EGame.GAME_UE4_25 => Type.MaterialShaderMapIdSerialization,
                < EGame.GAME_UE4_26 => Type.AutoExposureDefaultFix,
                < EGame.GAME_UE4_27 => Type.VolumeExtinctionBecomesRGB,
                _ => Type.LatestVersion
            };
        }
    }
}