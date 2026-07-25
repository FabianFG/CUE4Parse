using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Objects.UObject
{
    /** Flags used for texture creation */
    [Flags]
    public enum ETextureCreateFlags : uint
    {
        // Texture is encoded in sRGB gamma space
        TexCreate_SRGB					= 1<<0,
        // Texture can be used as a resolve target (normally not stored in the texture pool)
        TexCreate_ResolveTargetable		= 1<<1,
        // Texture is a depth stencil format that can be sampled
        TexCreate_DepthStencil			= 1<<2,
        // Texture will be created without a packed miptail
        TexCreate_NoMipTail				= 1<<3,
        // Texture will be created with an un-tiled format
        TexCreate_NoTiling				= 1<<4,
        // Texture that for a resolve target will only be written to/resolved once
        TexCreate_WriteOnce				= 1<<5,
        // Texture that may be updated every frame
        TexCreate_Dynamic				= 1<<6,
        // Texture that didn't go through the offline cooker (normally not stored in the texture pool)
        TexCreate_Uncooked				= 1<<7,
        // Allow silent texture creation failure
        TexCreate_AllowFailure			= 1<<8,
        // Disable automatic defragmentation if the initial texture memory allocation fails.
        TexCreate_DisableAutoDefrag		= 1<<9,
        // Create the texture with automatic -1..1 biasing
        TexCreate_BiasNormalMap			= 1<<10,
        // Create the texture with the flag that allows mip generation later, only applicable to D3D11
        TexCreate_GenerateMipCapable	= 1<<11,
        // A resolve textures that can be presented to screen
        TexCreate_Presentable			= 1<<12,
        // Texture is used as a resolvetarget for a multisampled surface. (Required for multisampled depth textures)
        TexCreate_Multisample			= 1<<13,
        // Texture should disable any filtering (NGP only, and is hopefully temporary)
        TexCreate_PointFilterNGP		= 1<<14,
        // This is a targetable resolve texture for a TargetSurfCreate_HighPerf, so should be fast to read if possible
        TexCreate_HighPerf				= 1<<15,
        // Texture has been created with an explicit address.
        TexCreate_ExplicitAddress		= 1<<16,
    };

    public struct FTextureAllocations
    {
        public int Width;
        public int Height;
        public int MipCount;
        [JsonConverter(typeof(StringEnumConverter))]
        public EPixelFormat Format;
        [JsonConverter(typeof(StringEnumConverter))]
        public ETextureCreateFlags CreateFlags;
        public int[] ExportIndices;

        public FTextureAllocations(FArchive Ar)
        {
            Width = Ar.Read<int>();
            Height = Ar.Read<int>();
            MipCount = Ar.Read<int>();
            Format = (EPixelFormat)Ar.Read<uint>();
            CreateFlags = Ar.Read<ETextureCreateFlags>();
            ExportIndices = Ar.ReadArray<int>();
        }
    }
}
