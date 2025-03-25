using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using AssetRipper.TextureDecoder.Bc;

using CUE4Parse.Compression;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.Utils;

using CUE4Parse_Conversion.Textures.ASTC;
using CUE4Parse_Conversion.Textures.BC;
using CUE4Parse_Conversion.Textures.DXT;

namespace CUE4Parse_Conversion.Textures;

public static class TextureDecoder
{
    public static bool UseAssetRipperTextureDecoder { get; set; } = false;

    public static CTexture? Decode(this UTexture2D texture, int maxMipSize, ETexturePlatform platform = ETexturePlatform.DesktopMobile) => texture.Decode(texture.GetMipByMaxSize(maxMipSize), platform);
    public static CTexture? Decode(this UTexture2D texture, ETexturePlatform platform = ETexturePlatform.DesktopMobile) => texture.Decode(texture.GetFirstMip(), platform);
    public static CTexture? Decode(this UTexture texture, ETexturePlatform platform = ETexturePlatform.DesktopMobile) => texture.Decode(texture.GetFirstMip(), platform);
    public static CTexture? Decode(this UTexture texture, FTexture2DMipMap? mip, ETexturePlatform platform = ETexturePlatform.DesktopMobile, int zLayer = 0)
    {
        if (texture.PlatformData is { FirstMipToSerialize: >= 0, VTData: { } vt } && vt.IsInitialized())
        {
            return DecodeVT(texture, vt);
        }

        if (mip != null)
        {
            var sizeX = mip.SizeX;
            var sizeY = mip.SizeY;
            var sizeZ = mip.SizeZ;

            if (texture.Format == EPixelFormat.PF_BC7)
            {
                sizeX = sizeX.Align(4);
                sizeY = sizeY.Align(4);
                sizeZ = sizeZ.Align(4);
            }

            DecodeTexture(mip, sizeX, sizeY, sizeZ, texture.Format, texture.IsNormalMap, platform, out var data, out var colorType);
            return new CTexture( sizeX, sizeY, colorType, data);
        }

        return null;
    }

    private static byte[] GetSliceData(byte[] data, int sizeX, int sizeY, int zLayer = 0)
    {
        var offset = sizeX * sizeY * 4;
        var startIndex = offset * zLayer;
        var endIndex = startIndex + offset;

        return endIndex > data.Length ? data : data[startIndex..endIndex];
    }

    private static CTexture DecodeVT(UTexture texture, FVirtualTextureBuiltData vt)
    {
        var tileSize = (int)vt.TileSize;
        var tileBorderSize = (int)vt.TileBorderSize;
        var tilePixelSize = (int)vt.GetPhysicalTileSize();
        const int level = 0;

        FVirtualTextureTileOffsetData tileOffsetData;
        if (vt.IsLegacyData())
        {
            var blockWidthInTiles = vt.GetWidthInTiles();
            var blockHeightInTiles = vt.GetHeightInTiles();
            var maxAddress = vt.TileIndexPerMip[Math.Min(level + 1, vt.NumMips)];
            tileOffsetData = new FVirtualTextureTileOffsetData(blockWidthInTiles, blockHeightInTiles, Math.Max(maxAddress - vt.TileIndexPerMip[level], 1));
        }
        else
        {
            tileOffsetData = vt.TileOffsetData[level];
        }

        var bitmapWidth = (int)tileOffsetData.Width * tileSize;
        var bitmapHeight = (int)tileOffsetData.Height * tileSize;
        var maxLevel = Math.Ceiling(Math.Log2(Math.Max(tileOffsetData.Width, tileOffsetData.Height)));
        if (tileOffsetData.MaxAddress > 1 && (maxLevel == 0 || vt.IsLegacyData()))
        {
            var baseLevel = vt.IsLegacyData() ? maxLevel : Math.Ceiling(Math.Log2(Math.Max(vt.TileOffsetData[0].Width, vt.TileOffsetData[0].Height)));
            var factor = Convert.ToInt32(Math.Max(Math.Pow(2, vt.IsLegacyData() ? level : level - baseLevel), 1));
            bitmapWidth /= factor;
            bitmapHeight /= factor;
        }

        var colorType = PixelFormat.PF_MAX;
        var bytesPerPixel = 0;
        var rowBytes = 0;
        var tileRowBytes = 0;
        var result = new byte[bitmapWidth * bitmapHeight * 4];

        for (uint layer = 0; layer < vt.NumLayers; layer++)
        {
            var layerFormat = vt.LayerTypes[layer];
            if (PixelFormatUtils.PixelFormats.ElementAtOrDefault((int)layerFormat) is not { Supported: true } formatInfo || formatInfo.BlockBytes == 0)
                throw new NotImplementedException($"The supplied pixel format {layerFormat} is not supported!");

            var tileWidthInBlocks = tilePixelSize.DivideAndRoundUp(formatInfo.BlockSizeX);
            var tileHeightInBlocks = tilePixelSize.DivideAndRoundUp(formatInfo.BlockSizeY);
            var packedStride = tileWidthInBlocks * formatInfo.BlockBytes;
            var packedOutputSize = packedStride * tileHeightInBlocks;

            var layerData = ArrayPool<byte>.Shared.Rent(packedOutputSize);

            for (uint tileIndexInMip = 0; tileIndexInMip < tileOffsetData.MaxAddress; tileIndexInMip++)
            {
                if (!vt.IsValidAddress(level, tileIndexInMip)) continue;

                var tileX = (int)MathUtils.ReverseMortonCode2(tileIndexInMip) * tileSize;
                var tileY = (int)MathUtils.ReverseMortonCode2(tileIndexInMip >> 1) * tileSize;
                var (chunkIndex, tileStart, tileLength) = vt.GetTileData(level, tileIndexInMip, layer);

                if (vt.Chunks[chunkIndex].CodecType[layer] == EVirtualTextureCodec.ZippedGPU_DEPRECATED)
                {
                    Compression.Decompress(vt.Chunks[chunkIndex].BulkData.Data!, (int)tileStart, (int)tileLength, layerData, 0, packedOutputSize, CompressionMethod.Zlib);
                }
                else
                {
                    Array.Copy(vt.Chunks[chunkIndex].BulkData.Data!, tileStart, layerData, 0, packedOutputSize);
                }

                DecodeBytes(layerData, tilePixelSize, tilePixelSize, 1, formatInfo, texture.IsNormalMap, out var data, out var tileColorType);

                if (colorType == PixelFormat.PF_MAX)
                {
                    colorType = tileColorType;
                    bytesPerPixel = formatInfo.BlockBytes;
                    rowBytes = bitmapWidth * bytesPerPixel;
                    tileRowBytes = tilePixelSize * bytesPerPixel;
                }
                else if (colorType != tileColorType)
                {
                    throw new NotSupportedException("multiple pixelformats/colortypes in a single virtual image is not supported");
                }

                for (int i = 0; i < tileSize; i++)
                {
                    var tileOffset = ((i + tileBorderSize) * tilePixelSize + tileBorderSize) * bytesPerPixel;
                    var offset = tileX * bytesPerPixel + (tileY + i) * rowBytes;
                    var srcSpan = data.AsSpan(tileOffset, tileRowBytes);
                    var destSpan = result.AsSpan(offset, tileRowBytes);
                    srcSpan.CopyTo(destSpan);
                }
            }

            ArrayPool<byte>.Shared.Return(layerData);
        }

        return new CTexture(bitmapWidth, bitmapHeight, colorType, GetSliceData(result, bitmapWidth, bitmapHeight));
    }

    public static CTexture[]? DecodeTextureArray(this UTexture2DArray texture, ETexturePlatform platform = ETexturePlatform.DesktopMobile)
    {
        var mip = texture.GetFirstMip();

        if (mip is null)
            return null;

        var sizeX = mip.SizeX;
        var sizeY = mip.SizeY;
        var sizeZ = mip.SizeZ;

        if (texture.Format == EPixelFormat.PF_BC7)
        {
            sizeX = sizeX.Align(4);
            sizeY = sizeY.Align(4);
            sizeZ = sizeZ.Align(4);
        }

        DecodeTexture(mip, sizeX, sizeY, sizeZ, texture.Format, texture.IsNormalMap, platform, out var data, out var colorType);

        var bitmaps = new List<CTexture>();
        var offset = sizeX * sizeY * 4;
        for (var i = 0; i < sizeZ; i++)
        {
            if (offset * (i + 1) > data.Length)
                break;
            bitmaps.Add(new CTexture(sizeX, sizeY, colorType, GetSliceData(data, sizeX, sizeY, i)));
        }

        return bitmaps.ToArray();
    }

    private static void DecodeTexture(FTexture2DMipMap? mip, int sizeX, int sizeY, int sizeZ, EPixelFormat format, bool isNormalMap, ETexturePlatform platform, out byte[] data, out PixelFormat colorType)
    {
        if (mip?.BulkData.Data is not { Length: > 0 })
            throw new ParserException("Supplied MipMap is null or has empty data!");
        if (PixelFormatUtils.PixelFormats.ElementAtOrDefault((int) format) is not { Supported: true } formatInfo || formatInfo.BlockBytes == 0)
            throw new NotImplementedException($"The supplied pixel format {format} is not supported!");

        var isXBPS = platform == ETexturePlatform.XboxAndPlaystation;
        var isNX = platform == ETexturePlatform.NintendoSwitch;

        // If the platform requires deswizzling, check if we should even try.
        if (isXBPS || isNX)
        {
            var blockSizeX = mip.SizeX / formatInfo.BlockSizeX;
            var blockSizeY = mip.SizeY / formatInfo.BlockSizeY;
            var totalBlocks = mip.BulkData.Data.Length / formatInfo.BlockBytes;
            if (blockSizeX * blockSizeY > totalBlocks)
                throw new ParserException("The supplied MipMap could not be untiled!");
        }

        var bytes = mip.BulkData.Data;

        // Handle deswizzling if necessary.
        if (isXBPS)
            bytes = PlatformDeswizzlers.DeswizzleXBPS(bytes, mip, formatInfo);
        else if (isNX)
            bytes = PlatformDeswizzlers.GetDeswizzledData(bytes, mip, formatInfo);

        DecodeBytes(bytes, sizeX, sizeY, sizeZ, formatInfo, isNormalMap, out data, out colorType);
    }

    private static void DecodeBytes(byte[] bytes, int sizeX, int sizeY, int sizeZ, FPixelFormatInfo formatInfo, bool isNormalMap, out byte[] data, out PixelFormat colorType)
    {
        switch (formatInfo.UnrealFormat)
        {
            case EPixelFormat.PF_DXT1:
            {
                if (UseAssetRipperTextureDecoder)
                {
                    Bc1.Decompress(bytes, sizeX, sizeY, out data);
                    colorType = PixelFormat.PF_RGBA8;
                }
                else
                {
                    data = DXTDecoder.DXT1(bytes, sizeX, sizeY, sizeZ);
                    colorType = PixelFormat.PF_RGBA8;
                }
                break;
            }
            case EPixelFormat.PF_DXT5:
                if (UseAssetRipperTextureDecoder)
                {
                    Bc3.Decompress(bytes, sizeX, sizeY, out data);
                    colorType = PixelFormat.PF_BGRA8;
                }
                else
                {
                    data = DXTDecoder.DXT5(bytes, sizeX, sizeY, sizeZ);
                    colorType = PixelFormat.PF_RGBA8;
                }
                break;
            case EPixelFormat.PF_ASTC_4x4:
            case EPixelFormat.PF_ASTC_6x6:
            case EPixelFormat.PF_ASTC_8x8:
            case EPixelFormat.PF_ASTC_10x10:
            case EPixelFormat.PF_ASTC_12x12:
                data = ASTCDecoder.RGBA8888(bytes, formatInfo.BlockSizeX, formatInfo.BlockSizeY, formatInfo.BlockSizeZ, sizeX, sizeY, sizeZ);
                colorType = PixelFormat.PF_RGBA8;

                if (isNormalMap)
                {
                    // UE4 drops blue channel for normal maps before encoding, restore it
                    unsafe
                    {
                        var offset = 0;
                        fixed (byte* d = data)
                        {
                            for (var i = 0; i < sizeX * sizeY; i++)
                            {
                                d[offset + 2] = BCDecoder.GetZNormal(d[offset], d[offset + 1]);
                                offset += 4;
                            }
                        }
                    }
                }

                break;
            case EPixelFormat.PF_BC4:
                if (UseAssetRipperTextureDecoder)
                {
                    Bc4.Decompress(bytes, sizeX, sizeY, out data);
                    colorType = PixelFormat.PF_BGRA8;
                }
                else
                {
                    data = BCDecoder.BC4(bytes, sizeX, sizeY, sizeZ);
                    colorType = PixelFormat.PF_RGBx8;
                }
                break;
            case EPixelFormat.PF_BC5:
                if (UseAssetRipperTextureDecoder)
                {
                    Bc5.Decompress(bytes, sizeX, sizeY, out data);
                    colorType = PixelFormat.PF_BGRA8;
                }
                else
                {
                    data = BCDecoder.BC5(bytes, sizeX, sizeY, sizeZ);
                    colorType = PixelFormat.PF_RGBx8;
                }
                break;
            case EPixelFormat.PF_BC6H:
                if (UseAssetRipperTextureDecoder)
                {
                    Bc6h.Decompress(bytes, sizeX, sizeY, false, out data);
                    colorType = PixelFormat.PF_BGRA8;
                }
                else
                {
                    // BC6H doesn't work no matter the pixel format, the closest we can get is either
                    // Rgb565 DETEX_PIXEL_FORMAT_FLOAT_RGBX16 or Rgb565 DETEX_PIXEL_FORMAT_FLOAT_BGRX16
                    data = Detex.DecodeDetexLinear(bytes, sizeX, sizeY, true, DetexTextureFormat.DETEX_TEXTURE_FORMAT_BPTC_FLOAT, DetexPixelFormat.DETEX_PIXEL_FORMAT_FLOAT_RGBX16);
                    colorType = PixelFormat.PF_BGRA8; // TODO SKColorType.Rgb565;
                }
                break;
            case EPixelFormat.PF_BC7:
                if (UseAssetRipperTextureDecoder)
                {
                    Bc7.Decompress(bytes, sizeX, sizeY, out data);
                    colorType = PixelFormat.PF_BGRA8;
                }
                else
                {
                    data = Detex.DecodeDetexLinear(bytes, sizeX, sizeY, false, DetexTextureFormat.DETEX_TEXTURE_FORMAT_BPTC, DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                    colorType = PixelFormat.PF_RGBA8;
                }
                break;
            case EPixelFormat.PF_ETC1:
                data = Detex.DecodeDetexLinear(bytes, sizeX, sizeY, false, DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC1, DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                colorType = PixelFormat.PF_RGBA8;
                break;
            case EPixelFormat.PF_ETC2_RGB:
                data = Detex.DecodeDetexLinear(bytes, sizeX, sizeY, false, DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC2, DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                colorType = PixelFormat.PF_RGBA8;
                break;
            case EPixelFormat.PF_ETC2_RGBA:
                data = Detex.DecodeDetexLinear(bytes, sizeX, sizeY, false, DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC2_EAC, DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                colorType = PixelFormat.PF_RGBA8;
                break;
            case EPixelFormat.PF_B8G8R8A8:
                data = bytes;
                colorType = PixelFormat.PF_BGRA8;
                break;
            case EPixelFormat.PF_G8:
                data = bytes;
                colorType = PixelFormat.PF_R8;
                break;
            case EPixelFormat.PF_G16:
                data = bytes;
                colorType = PixelFormat.PF_R16;
                break;
            case EPixelFormat.PF_R32G32B32F:
                data = bytes;
                colorType = PixelFormat.PF_RGB32F;
                break;
            case EPixelFormat.PF_R16F:
            case EPixelFormat.PF_R16F_FILTER:
                data = bytes;
                unsafe
                {
                    fixed (byte* d = bytes) //Convert Unreal 16bit half float to 32bit float
                        data = ConvertHalfToFloat(sizeX, sizeY, sizeZ, d, 1);
                }
                colorType = PixelFormat.PF_R32F;
                break;
            case EPixelFormat.PF_FloatRGBA:
                unsafe
                {
                    fixed (byte* d = bytes) //Convert Unreal 16bit half float to 32bit float
                        data = ConvertHalfToFloat(sizeX, sizeY, sizeZ, d, 4);
                }
                colorType = PixelFormat.PF_RGBA32F;
                break;
            default: throw new NotImplementedException($"Unknown pixel format: {formatInfo.UnrealFormat}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe byte[] ConvertHalfToFloat(int width, int height, int depth, byte* inp, int channels)
    {
        int srcPitch = width * channels * sizeof(ushort); // channels * 2 bytes (16-bit per channel)

        int totalSize = width * height * depth * channels * sizeof(float); // channels * 4 bytes (32-bit per channel)
        byte[] ret = new byte[totalSize];

        fixed (byte* outPtr = ret)
        {
            for (var z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    Half* srcRowPtr = (Half*)(inp + z * height * srcPitch + y * srcPitch);
                    float* destRowPtr = (float*)(outPtr + z * height * width * channels * sizeof(float) + y * width * channels * sizeof(float));

                    for (int x = 0; x < width; x++)
                        for (int c = 0; c < channels; c++)
                            *destRowPtr++ = (float)*srcRowPtr++;
                }
            }
        }
        return ret;
    }

}
