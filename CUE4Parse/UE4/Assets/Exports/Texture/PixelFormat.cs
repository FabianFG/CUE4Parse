
namespace CUE4Parse.UE4.Assets.Exports.Texture;

public static class PixelFormatUtils
{
    //Pixelformat.h IsHDR
    public static bool IsHDR(EPixelFormat pixelFormat) => pixelFormat is EPixelFormat.PF_FloatRGBA
                                                                        or EPixelFormat.PF_BC6H
                                                                        or EPixelFormat.PF_R16F
                                                                        or EPixelFormat.PF_R32_FLOAT
                                                                        or EPixelFormat.PF_A32B32G32R32F
                                                                        or EPixelFormat.PF_ASTC_4x4_HDR
                                                                        or EPixelFormat.PF_ASTC_6x6_HDR
                                                                        or EPixelFormat.PF_ASTC_8x8_HDR
                                                                        or EPixelFormat.PF_ASTC_10x10_HDR
                                                                        or EPixelFormat.PF_ASTC_12x12_HDR;

    public static Dictionary<EPixelFormat, FPixelFormatInfo> PixelFormats = new Dictionary<EPixelFormat, FPixelFormatInfo>
    {
        //       Pixel Format                           Pixel Format                     Name               BlockSizeX  BlockSizeY  BlockSizeZ  BlockBytes  NumComponents  Supported by CUE4Parse
        { EPixelFormat.PF_Unknown,            new(EPixelFormat.PF_Unknown,            "unknown",                0,          0,          0,          0,            0,                false) },
        { EPixelFormat.PF_A32B32G32R32F,      new(EPixelFormat.PF_A32B32G32R32F,      "A32B32G32R32F",          1,          1,          1,          16,           4,                true) },
        { EPixelFormat.PF_B8G8R8A8,           new(EPixelFormat.PF_B8G8R8A8,           "B8G8R8A8",               1,          1,          1,          4,            4,                true) },
        { EPixelFormat.PF_G8,                 new(EPixelFormat.PF_G8,                 "G8",                     1,          1,          1,          1,            1,                true) },
        { EPixelFormat.PF_G16,                new(EPixelFormat.PF_G16,                "G16",                    1,          1,          1,          2,            1,                true) },
        { EPixelFormat.PF_DXT1,               new(EPixelFormat.PF_DXT1,               "DXT1",                   4,          4,          1,          8,            3,                true) },
        { EPixelFormat.PF_DXT3,               new(EPixelFormat.PF_DXT3,               "DXT3",                   4,          4,          1,          16,           4,                true) },
        { EPixelFormat.PF_DXT5,               new(EPixelFormat.PF_DXT5,               "DXT5",                   4,          4,          1,          16,           4,                true) },
        { EPixelFormat.PF_UYVY,               new(EPixelFormat.PF_UYVY,               "UYVY",                   2,          1,          1,          4,            4,                false) },
        { EPixelFormat.PF_FloatRGB,           new(EPixelFormat.PF_FloatRGB,           "FloatRGB",               1,          1,          1,          4,            3,                true) },
        { EPixelFormat.PF_FloatRGBA,          new(EPixelFormat.PF_FloatRGBA,          "FloatRGBA",              1,          1,          1,          8,            4,                true) },
        { EPixelFormat.PF_DepthStencil,       new(EPixelFormat.PF_DepthStencil,       "DepthStencil",           1,          1,          1,          4,            1,                false) },
        { EPixelFormat.PF_ShadowDepth,        new(EPixelFormat.PF_ShadowDepth,        "ShadowDepth",            1,          1,          1,          4,            1,                false) },
        { EPixelFormat.PF_R32_FLOAT,          new(EPixelFormat.PF_R32_FLOAT,          "R32_FLOAT",              1,          1,          1,          4,            1,                true) },
        { EPixelFormat.PF_G16R16,             new(EPixelFormat.PF_G16R16,             "G16R16",                 1,          1,          1,          4,            2,                true) },
        { EPixelFormat.PF_G16R16F,            new(EPixelFormat.PF_G16R16F,            "G16R16F",                1,          1,          1,          4,            2,                true) },
        { EPixelFormat.PF_G16R16F_FILTER,     new(EPixelFormat.PF_G16R16F_FILTER,     "G16R16F_FILTER",         1,          1,          1,          4,            2,                true) },
        { EPixelFormat.PF_G32R32F,            new(EPixelFormat.PF_G32R32F,            "G32R32F",                1,          1,          1,          8,            2,                true) },
        { EPixelFormat.PF_A2B10G10R10,        new(EPixelFormat.PF_A2B10G10R10,        "A2B10G10R10",            1,          1,          1,          4,            4,                false) },
        { EPixelFormat.PF_A16B16G16R16,       new(EPixelFormat.PF_A16B16G16R16,       "A16B16G16R16",           1,          1,          1,          8,            4,                true) },
        { EPixelFormat.PF_D24,                new(EPixelFormat.PF_D24,                "D24",                    1,          1,          1,          4,            1,                false) },
        { EPixelFormat.PF_R16F,               new(EPixelFormat.PF_R16F,               "PF_R16F",                1,          1,          1,          2,            1,                true) },
        { EPixelFormat.PF_R16F_FILTER,        new(EPixelFormat.PF_R16F_FILTER,        "PF_R16F_FILTER",         1,          1,          1,          2,            1,                true) },
        { EPixelFormat.PF_BC5,                new(EPixelFormat.PF_BC5,                "BC5",                    4,          4,          1,          16,           2,                true) },
        { EPixelFormat.PF_V8U8,               new(EPixelFormat.PF_V8U8,               "V8U8",                   1,          1,          1,          2,            2,                true) },
        { EPixelFormat.PF_A1,                 new(EPixelFormat.PF_A1,                 "A1",                     1,          1,          1,          1,            1,                false) },
        { EPixelFormat.PF_FloatR11G11B10,     new(EPixelFormat.PF_FloatR11G11B10,     "FloatR11G11B10",         1,          1,          1,          4,            3,                false) },
        { EPixelFormat.PF_A8,                 new(EPixelFormat.PF_A8,                 "A8",                     1,          1,          1,          1,            1,                false) },
        { EPixelFormat.PF_R32_UINT,           new(EPixelFormat.PF_R32_UINT,           "R32_UINT",               1,          1,          1,          4,            1,                false) },
        { EPixelFormat.PF_R32_SINT,           new(EPixelFormat.PF_R32_SINT,           "R32_SINT",               1,          1,          1,          4,            1,                false) },

        { EPixelFormat.PF_PVRTC2,             new(EPixelFormat.PF_PVRTC2,             "PVRTC2",                 8,          4,          1,          8,            4,                true) },
        { EPixelFormat.PF_PVRTC4,             new(EPixelFormat.PF_PVRTC4,             "PVRTC4",                 4,          4,          1,          8,            4,                true) },

        { EPixelFormat.PF_R16_UINT,           new(EPixelFormat.PF_R16_UINT,           "R16_UINT",               1,          1,          1,          2,            1,                false) },
        { EPixelFormat.PF_R16_SINT,           new(EPixelFormat.PF_R16_SINT,           "R16_SINT",               1,          1,          1,          2,            1,                false) },
        { EPixelFormat.PF_R16G16B16A16_UINT,  new(EPixelFormat.PF_R16G16B16A16_UINT,  "R16G16B16A16_UINT",      1,          1,          1,          8,            4,                false) },
        { EPixelFormat.PF_R16G16B16A16_SINT,  new(EPixelFormat.PF_R16G16B16A16_SINT,  "R16G16B16A16_SINT",      1,          1,          1,          8,            4,                false) },
        { EPixelFormat.PF_R5G6B5_UNORM,       new(EPixelFormat.PF_R5G6B5_UNORM,       "PF_R5G6B5_UNORM",        1,          1,          1,          2,            3,                false) },
        { EPixelFormat.PF_R8G8B8A8,           new(EPixelFormat.PF_R8G8B8A8,           "R8G8B8A8",               1,          1,          1,          4,            4,                true) },
        { EPixelFormat.PF_A8R8G8B8,           new(EPixelFormat.PF_A8R8G8B8,           "A8R8G8B8",               1,          1,          1,          4,            4,                true) },
        { EPixelFormat.PF_BC4,                new(EPixelFormat.PF_BC4,                "BC4",                    4,          4,          1,          8,            1,                true) },
        { EPixelFormat.PF_R8G8,               new(EPixelFormat.PF_R8G8,               "R8G8",                   1,          1,          1,          2,            2,                false) },

        { EPixelFormat.PF_ATC_RGB,            new(EPixelFormat.PF_ATC_RGB,            "ATC_RGB",                4,          4,          1,          8,            3,                false) },
        { EPixelFormat.PF_ATC_RGBA_E,         new(EPixelFormat.PF_ATC_RGBA_E,         "ATC_RGBA_E",             4,          4,          1,          16,           4,                false) },
        { EPixelFormat.PF_ATC_RGBA_I,         new(EPixelFormat.PF_ATC_RGBA_I,         "ATC_RGBA_I",             4,          4,          1,          16,           4,                false) },
        { EPixelFormat.PF_X24_G8,             new(EPixelFormat.PF_X24_G8,             "X24_G8",                 1,          1,          1,          1,            1,                false) },
        { EPixelFormat.PF_ETC1,               new(EPixelFormat.PF_ETC1,               "ETC1",                   4,          4,          1,          8,            3,                true) },
        { EPixelFormat.PF_ETC2_RGB,           new(EPixelFormat.PF_ETC2_RGB,           "ETC2_RGB",               4,          4,          1,          8,            3,                true) },
        { EPixelFormat.PF_ETC2_RGBA,          new(EPixelFormat.PF_ETC2_RGBA,          "ETC2_RGBA",              4,          4,          1,          16,           4,                true) },
        { EPixelFormat.PF_R32G32B32A32_UINT,  new(EPixelFormat.PF_R32G32B32A32_UINT,  "PF_R32G32B32A32_UINT",   1,          1,          1,          16,           4,                false) },
        { EPixelFormat.PF_R16G16_UINT,        new(EPixelFormat.PF_R16G16_UINT,        "PF_R16G16_UINT",         1,          1,          1,          4,            4,                false) },

        { EPixelFormat.PF_ASTC_4x4,           new(EPixelFormat.PF_ASTC_4x4,           "ASTC_4x4",               4,          4,          1,          16,           4,                true) },
        { EPixelFormat.PF_ASTC_6x6,           new(EPixelFormat.PF_ASTC_6x6,           "ASTC_6x6",               6,          6,          1,          16,           4,                true) },
        { EPixelFormat.PF_ASTC_8x8,           new(EPixelFormat.PF_ASTC_8x8,           "ASTC_8x8",               8,          8,          1,          16,           4,                true) },
        { EPixelFormat.PF_ASTC_10x10,         new(EPixelFormat.PF_ASTC_10x10,         "ASTC_10x10",             10,         10,         1,          16,           4,                true) },
        { EPixelFormat.PF_ASTC_12x12,         new(EPixelFormat.PF_ASTC_12x12,         "ASTC_12x12",             12,         12,         1,          16,           4,                true) },
        
        { EPixelFormat.PF_BC6H,               new(EPixelFormat.PF_BC6H,               "BC6H",                   4,          4,          1,          16,           3,                true) },
        { EPixelFormat.PF_BC7,                new(EPixelFormat.PF_BC7,                "BC7",                    4,          4,          1,          16,           4,                true) },
        { EPixelFormat.PF_R8_UINT,            new(EPixelFormat.PF_R8_UINT,            "R8_UINT",                1,          1,          1,          1,            1,                false) },
        { EPixelFormat.PF_L8,                 new(EPixelFormat.PF_L8,                 "L8",                     1,          1,          1,          1,            1,                false) },
        { EPixelFormat.PF_XGXR8,              new(EPixelFormat.PF_XGXR8,              "XGXR8",                  1,          1,          1,          4,            4,                false) },
        { EPixelFormat.PF_R8G8B8A8_UINT,      new(EPixelFormat.PF_R8G8B8A8_UINT,      "R8G8B8A8_UINT",          1,          1,          1,          4,            4,                false) },
        { EPixelFormat.PF_R8G8B8A8_SNORM,     new(EPixelFormat.PF_R8G8B8A8_SNORM,     "R8G8B8A8_SNORM",         1,          1,          1,          4,            4,                false) },

        { EPixelFormat.PF_R16G16B16A16_UNORM, new(EPixelFormat.PF_R16G16B16A16_UNORM, "R16G16B16A16_UINT",      1,          1,          1,          8,            4,                false) },
        { EPixelFormat.PF_R16G16B16A16_SNORM, new(EPixelFormat.PF_R16G16B16A16_SNORM, "R16G16B16A16_SINT",      1,          1,          1,          8,            4,                false) },
        { EPixelFormat.PF_PLATFORM_HDR_0,     new(EPixelFormat.PF_PLATFORM_HDR_0,     "PLATFORM_HDR_0",         0,          0,          0,          0,            0,                false) },
        { EPixelFormat.PF_PLATFORM_HDR_1,     new(EPixelFormat.PF_PLATFORM_HDR_1,     "PLATFORM_HDR_1",         0,          0,          0,          0,            0,                false) },
        { EPixelFormat.PF_PLATFORM_HDR_2,     new(EPixelFormat.PF_PLATFORM_HDR_2,     "PLATFORM_HDR_2",         0,          0,          0,          0,            0,                false) },

        // NV12 contains 2 textures: R8 luminance plane followed by R8G8 1/4 size chrominance plane.
        // BlockSize/BlockBytes/NumComponents values don't make much sense for this format, so set them all to one.
        { EPixelFormat.PF_NV12,               new(EPixelFormat.PF_NV12,               "NV12",                   1,          1,          1,          1,            1,                false) },

        { EPixelFormat.PF_R32G32_UINT,        new(EPixelFormat.PF_R32G32_UINT,        "PF_R32G32_UINT",         1,          1,          1,          8,            2,                false) },

        { EPixelFormat.PF_ETC2_R11_EAC,       new(EPixelFormat.PF_ETC2_R11_EAC,       "PF_ETC2_R11_EAC",        4,          4,          1,          8,            1,                false) },
        { EPixelFormat.PF_ETC2_RG11_EAC,      new(EPixelFormat.PF_ETC2_RG11_EAC,      "PF_ETC2_RG11_EAC",       4,          4,          1,          16,           2,                false) },
        { EPixelFormat.PF_R8,                 new(EPixelFormat.PF_R8,                 "R8",                     1,          1,          1,          1,            1,                false) },
        { EPixelFormat.PF_B5G5R5A1_UNORM,     new(EPixelFormat.PF_B5G5R5A1_UNORM,     "B5G5R5A1_UNORM",         1,          1,          1,          2,            4,                false) },

        // ASTC HDR support
        { EPixelFormat.PF_ASTC_4x4_HDR,       new(EPixelFormat.PF_ASTC_4x4_HDR,       "ASTC_4x4_HDR",           4,          4,          1,          16,           4,                false) },
        { EPixelFormat.PF_ASTC_6x6_HDR,       new(EPixelFormat.PF_ASTC_6x6_HDR,       "ASTC_6x6_HDR",           6,          6,          1,          16,           4,                false) },
        { EPixelFormat.PF_ASTC_8x8_HDR,       new(EPixelFormat.PF_ASTC_8x8_HDR,       "ASTC_8x8_HDR",           8,          8,          1,          16,           4,                false) },
        { EPixelFormat.PF_ASTC_10x10_HDR,     new(EPixelFormat.PF_ASTC_10x10_HDR,     "ASTC_10x10_HDR",         10,         10,         1,          16,           4,                false) },
        { EPixelFormat.PF_ASTC_12x12_HDR,     new(EPixelFormat.PF_ASTC_12x12_HDR,     "ASTC_12x12_HDR",         12,         12,         1,          16,           4,                false) },

        { EPixelFormat.PF_G16R16_SNORM,       new(EPixelFormat.PF_G16R16_SNORM,       "G16R16_SNORM",           1,          1,          1,          4,            2,                false) },
        { EPixelFormat.PF_R8G8_UINT,          new(EPixelFormat.PF_R8G8_UINT,          "R8G8_UINT",              1,          1,          1,          2,            2,                false) },
        { EPixelFormat.PF_R32G32B32_UINT,     new(EPixelFormat.PF_R32G32B32_UINT,     "R32G32B32_UINT",         1,          1,          1,          12,           3,                false) },
        { EPixelFormat.PF_R32G32B32_SINT,     new(EPixelFormat.PF_R32G32B32_SINT,     "R32G32B32_SINT",         1,          1,          1,          12,           3,                false) },
        { EPixelFormat.PF_R32G32B32F,         new(EPixelFormat.PF_R32G32B32F,         "R32G32B32F",             1,          1,          1,          12,           3,                false) },
        { EPixelFormat.PF_R8_SINT,            new(EPixelFormat.PF_R8_SINT,            "R8_SINT",                1,          1,          1,          1,            1,                false) },
        { EPixelFormat.PF_R64_UINT,           new(EPixelFormat.PF_R64_UINT,           "R64_UINT",               1,          1,          1,          8,            1,                false) },
        { EPixelFormat.PF_R9G9B9EXP5,         new(EPixelFormat.PF_R9G9B9EXP5,         "R9G9B9EXP5",             1,          1,          1,          4,            4,                false) },

        // P010 contains 2 textures: R16 luminance plane followed by R16G16 1/4 size chrominance plane. (upper 10 bits used)
        // BlockSize/BlockBytes/NumComponents values don't make much sense for this format, so set them all to one.
        { EPixelFormat.PF_P010,               new(EPixelFormat.PF_P010,               "P010",                   1,          1,          1,          2,            1,                false) },

        // ASTC high precision NormalRG support
        { EPixelFormat.PF_ASTC_4x4_NORM_RG,   new(EPixelFormat.PF_ASTC_4x4_NORM_RG,   "ASTC_4x4_NORM_RG",       4,          4,          1,          16,           2,                false) },
        { EPixelFormat.PF_ASTC_6x6_NORM_RG,   new(EPixelFormat.PF_ASTC_6x6_NORM_RG,   "ASTC_6x6_NORM_RG",       6,          6,          1,          16,           2,                false) },
        { EPixelFormat.PF_ASTC_8x8_NORM_RG,   new(EPixelFormat.PF_ASTC_8x8_NORM_RG,   "ASTC_8x8_NORM_RG",       8,          8,          1,          16,           2,                false) },
        { EPixelFormat.PF_ASTC_10x10_NORM_RG, new(EPixelFormat.PF_ASTC_10x10_NORM_RG, "ASTC_10x10_NORM_RG",     10,         10,         1,          16,           2,                false) },
        { EPixelFormat.PF_ASTC_12x12_NORM_RG, new(EPixelFormat.PF_ASTC_12x12_NORM_RG, "ASTC_12x12_NORM_RG",     12,         12,         1,          16,           2,                false) },

        { EPixelFormat.PF_R16G16_SINT,        new(EPixelFormat.PF_R16G16_SINT,        "PF_R16G16_SINT",         1,          1,          1,          4,            4,                false) },
        { EPixelFormat.PF_R8G8B8,             new(EPixelFormat.PF_R8G8B8,             "R8G8B8",                 1,          1,          1,          3,            3,                false) },

        //Custom
        { EPixelFormat.PF_ASTC_8x5,           new(EPixelFormat.PF_ASTC_8x5,           "PF_ASTC_8x5",            8,          5,          1,          16,           4,                true) },
        { EPixelFormat.PF_ASTC_8x6,           new(EPixelFormat.PF_ASTC_8x6,           "PF_ASTC_8x6",            8,          6,          1,          16,           4,                true) },
        { EPixelFormat.PF_ASTC_10x8,          new(EPixelFormat.PF_ASTC_10x8,          "PF_ASTC_10x8",          10,          8,          1,          16,           4,                true) },
    };
}

public record FPixelFormatInfo(EPixelFormat UnrealFormat, string Name, int BlockSizeX, int BlockSizeY, int BlockSizeZ, int BlockBytes, int NumComponents, bool Supported)
{
    public int GetBlockCountForWidth(int width)
    {
        if (BlockSizeX > 0)
        {
            return (width + BlockSizeX - 1) / BlockSizeX;
        }

        return 0;
    }

    public int GetBlockCountForHeight(int height)
    {
        if (BlockSizeY > 0)
        {
            return (height + BlockSizeY - 1) / BlockSizeY;
        }

        return 0;
    }

    public int GetBlockCountForDepth(int depth)
    {
        if (BlockSizeZ > 0)
        {
            return (depth + BlockSizeZ - 1) / BlockSizeZ;
        }

        return 0;
    }

    public int Get2DImageSizeInBytes(int width, int height)
    {
        var blockWidth = GetBlockCountForWidth(width);
        var blockHeight = GetBlockCountForHeight(height);
        return blockWidth * blockHeight * BlockBytes;
    }

    public int Get2DTextureMipSizeInBytes(int width, int height, int mipIdx)
    {
        var mipWidth = Math.Max(width >> mipIdx, 1);
        var mipHeight = Math.Max(height >> mipIdx, 1);
        return Get2DImageSizeInBytes(mipWidth, mipHeight);
    }

    public int Get2DTextureSizeInBytes(int width, int height, int mipCount)
    {
        var size = 0;
        var mipWidth = width;
        var mipHeight = height;
        for (var idx = 0; idx < mipCount; ++idx)
        {
            size += Get2DImageSizeInBytes(mipWidth, mipHeight);
            mipWidth = Math.Max(mipWidth >> 1, 1);
            mipHeight = Math.Max(mipHeight >> 1, 1);
        }

        return size;
    }
}

public enum EPixelFormat : byte
{
    PF_Unknown              = 0,
    PF_A32B32G32R32F        = 1,
    PF_B8G8R8A8             = 2,
    PF_G8                   = 3, // G8  means Gray/Grey , not Green , typically actually uses a red format with replication of R to RGB
    PF_G16                  = 4, // G16 means Gray/Grey like G8
    PF_DXT1                 = 5,
    PF_DXT3                 = 6,
    PF_DXT5                 = 7,
    PF_UYVY                 = 8,
    PF_FloatRGB             = 9,  // 16F
    PF_FloatRGBA            = 10, // 16F
    PF_DepthStencil         = 11,
    PF_ShadowDepth          = 12,
    PF_R32_FLOAT            = 13,
    PF_G16R16               = 14,
    PF_G16R16F              = 15,
    PF_G16R16F_FILTER       = 16,
    PF_G32R32F              = 17,
    PF_A2B10G10R10          = 18,
    PF_A16B16G16R16         = 19,
    PF_D24                  = 20,
    PF_R16F                 = 21,
    PF_R16F_FILTER          = 22,
    PF_BC5                  = 23,
    PF_V8U8                 = 24,
    PF_A1                   = 25,
    PF_FloatR11G11B10       = 26,
    PF_A8                   = 27,
    PF_R32_UINT             = 28,
    PF_R32_SINT             = 29,
    PF_PVRTC2               = 30,
    PF_PVRTC4               = 31,
    PF_R16_UINT             = 32,
    PF_R16_SINT             = 33,
    PF_R16G16B16A16_UINT    = 34,
    PF_R16G16B16A16_SINT    = 35,
    PF_R5G6B5_UNORM         = 36,
    PF_R8G8B8A8             = 37,
    PF_A8R8G8B8				= 38,
    PF_BC4					= 39,
    PF_R8G8                 = 40,
    PF_ATC_RGB				= 41,	// Unsupported Format
    PF_ATC_RGBA_E			= 42,	// Unsupported Format
    PF_ATC_RGBA_I			= 43,	// Unsupported Format
    PF_X24_G8				= 44,	// Used for creating SRVs to alias a DepthStencil buffer to read Stencil. Don't use for creating textures.
    PF_ETC1					= 45,	// Unsupported Format
    PF_ETC2_RGB				= 46,
    PF_ETC2_RGBA			= 47,
    PF_R32G32B32A32_UINT	= 48,
    PF_R16G16_UINT			= 49,
    PF_ASTC_4x4             = 50,	// 8.00 bpp
    PF_ASTC_6x6             = 51,	// 3.56 bpp
    PF_ASTC_8x8             = 52,	// 2.00 bpp
    PF_ASTC_10x10           = 53,	// 1.28 bpp
    PF_ASTC_12x12           = 54,	// 0.89 bpp
    PF_BC6H					= 55,
    PF_BC7					= 56,
    PF_R8_UINT				= 57,
    PF_L8					= 58,
    PF_XGXR8				= 59,
    PF_R8G8B8A8_UINT		= 60,
    PF_R8G8B8A8_SNORM		= 61,
    PF_R16G16B16A16_UNORM	= 62,
    PF_R16G16B16A16_SNORM	= 63,
    PF_PLATFORM_HDR_0		= 64,
    PF_PLATFORM_HDR_1		= 65,	// Reserved.
    PF_PLATFORM_HDR_2		= 66,	// Reserved.
    PF_NV12					= 67,
    PF_R32G32_UINT          = 68,
    PF_ETC2_R11_EAC			= 69,
    PF_ETC2_RG11_EAC		= 70,
    PF_R8		            = 71,
    PF_B5G5R5A1_UNORM       = 72,
    PF_ASTC_4x4_HDR         = 73,
    PF_ASTC_6x6_HDR         = 74,
    PF_ASTC_8x8_HDR         = 75,
    PF_ASTC_10x10_HDR       = 76,
    PF_ASTC_12x12_HDR       = 77,
    PF_G16R16_SNORM			= 78,
    PF_R8G8_UINT			= 79,
    PF_R32G32B32_UINT		= 80,
    PF_R32G32B32_SINT		= 81,
    PF_R32G32B32F			= 82,
    PF_R8_SINT				= 83,
    PF_R64_UINT				= 84,
    PF_R9G9B9EXP5			= 85,
    PF_P010					= 86,
    PF_ASTC_4x4_NORM_RG     = 87, // RG format stored in LA endpoints for better precision (requires RHI support for texture swizzle)
    PF_ASTC_6x6_NORM_RG     = 88,
    PF_ASTC_8x8_NORM_RG     = 89,
    PF_ASTC_10x10_NORM_RG   = 90,
    PF_ASTC_12x12_NORM_RG   = 91,
    PF_R16G16_SINT          = 92,
    PF_R8G8B8               = 93,
    PF_MAX                  = 94,

    // Custom
    PF_ASTC_8x5             = 253,
    PF_ASTC_8x6             = 254,
    PF_ASTC_10x8            = 255,
}
