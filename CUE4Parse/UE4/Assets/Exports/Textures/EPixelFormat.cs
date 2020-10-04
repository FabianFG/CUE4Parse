namespace CUE4Parse.UE4.Assets.Exports.Textures
{
    public enum EPixelFormat
    {
        PF_Unknown,
        PF_A32B32G32R32F,
        PF_A8R8G8B8,			// exists in UE4, but marked as not used
        PF_G8,
        PF_G16,
        PF_DXT1,
        PF_DXT3,
        PF_DXT5,
        PF_UYVY,
        PF_FloatRGB,			// A RGB FP format with platform-specific implementation, for use with render targets
        PF_FloatRGBA,			// A RGBA FP format with platform-specific implementation, for use with render targets
        PF_DepthStencil,		// A depth+stencil format with platform-specific implementation, for use with render targets
        PF_ShadowDepth,			// A depth format with platform-specific implementation, for use with render targets
        PF_FilteredShadowDepth, // not in UE4
        PF_R32F,				// UE3
        PF_G16R16,
        PF_G16R16F,
        PF_G16R16F_FILTER,
        PF_G32R32F,
        PF_A2B10G10R10,
        PF_A16B16G16R16,
        PF_D24,
        PF_R16F,
        PF_R16F_FILTER,
        PF_BC5,
        PF_V8U8,
        PF_A1,
        PF_FloatR11G11B10,
        PF_A4R4G4B4,			// not in UE4
//#if UNREAL4
		PF_B8G8R8A8,			// new name for PF_A8R8G8B8
		PF_R32FLOAT,			// == PF_R32F in UE4
		PF_A8,
		PF_R32_UINT,
		PF_R32_SINT,
		PF_PVRTC2,
		PF_PVRTC4,
		PF_R16_UINT,
		PF_R16_SINT,
		PF_R16G16B16A16_UINT,
		PF_R16G16B16A16_SINT,
		PF_R5G6B5_UNORM,
		PF_R8G8B8A8,
	//	PF_A8R8G8B8,
		PF_BC4,
		PF_R8G8,
		PF_ATC_RGB,
		PF_ATC_RGBA_E,
		PF_ATC_RGBA_I,
		PF_X24_G8,
		PF_ETC1,
		PF_ETC2_RGB,
		PF_ETC2_RGBA,
		PF_R32G32B32A32_UINT,
		PF_R16G16_UINT,
		PF_ASTC_4x4,
		PF_ASTC_6x6,
		PF_ASTC_8x8,
		PF_ASTC_10x10,
		PF_ASTC_12x12,
		PF_BC6H,
		PF_BC7,
//#endif // UNREAL4
//#if GEARSU
		PF_DXN,
//#endif
//#if MASSEFF
		PF_NormalMap_LQ,
		PF_NormalMap_HQ,
//#endif
    }
}