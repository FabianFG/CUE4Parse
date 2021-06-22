using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;


namespace CUE4Parse.UE4.Versions
{
    class FRenderingObjectVersion
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

			// -----<new versions can be added above this line>-------------------------------------------------
			VersionPlusOne,
			LatestVersion = VersionPlusOne - 1
		}

		public static readonly FGuid GUID = new (0x12F88B9F, 0x88754AFC, 0xA67CD90C, 0x383ABD29);
		public static Type Get(FAssetArchive Ar)
		{
			int ver = VersionUtils.GetUE4CustomVersion(Ar, GUID);
			if (ver >= 0)
			return (Type)ver;

			if (Ar.Game < EGame.GAME_UE4_12)
				return Type.BeforeCustomVersionWasAdded;
			if (Ar.Game < EGame.GAME_UE4_13)
				return Type.CustomReflectionCaptureResolutionSupport;
			if (Ar.Game < EGame.GAME_UE4_14)
				return Type.IntroducedMeshDecals;
			if (Ar.Game < EGame.GAME_UE4_16) //  4.14 and 4.15
				return Type.MotionBlurAndTAASupportInSceneCapture2d;
			if (Ar.Game < EGame.GAME_UE4_17)
				return Type.ShaderResourceCodeSharing;
			if (Ar.Game < EGame.GAME_UE4_18)
				return Type.AddedbUseShowOnlyList;
			if (Ar.Game < EGame.GAME_UE4_19)
				return Type.VolumetricLightmaps;
			if (Ar.Game < EGame.GAME_UE4_20)
				return Type.ShaderPermutationId;
			if (Ar.Game < EGame.GAME_UE4_21)
				return Type.IncreaseNormalPrecision;
			if (Ar.Game < EGame.GAME_UE4_22)
				return Type.VirtualTexturedLightmaps;
			if (Ar.Game < EGame.GAME_UE4_23)
				return Type.GeometryCacheFastDecoder;
			if (Ar.Game < EGame.GAME_UE4_24)
				return Type.VirtualTexturedLightmapsV2;
			if (Ar.Game < EGame.GAME_UE4_25)
				return Type.MaterialShaderMapIdSerialization;
			if (Ar.Game < EGame.GAME_UE4_26)
				return Type.AutoExposureDefaultFix;
			return Type.LatestVersion;
		}
	}

}
