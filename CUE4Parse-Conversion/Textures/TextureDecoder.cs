using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse_Conversion.Textures.ASTC;
using CUE4Parse_Conversion.Textures.BC;
using CUE4Parse_Conversion.Textures.DXT;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.Utils;
using SkiaSharp;
using static CUE4Parse.Utils.TypeConversionUtils;

namespace CUE4Parse_Conversion.Textures;

public static class TextureDecoder
{
    private static readonly ArrayPool<byte> _shared = ArrayPool<byte>.Shared;

    public static SKBitmap? Decode(this UTexture2D texture, int maxMipSize, ETexturePlatform platform = ETexturePlatform.DesktopMobile) => texture.Decode(texture.GetMipByMaxSize(maxMipSize), platform);
    public static SKBitmap? Decode(this UTexture2D texture, ETexturePlatform platform = ETexturePlatform.DesktopMobile) => texture.Decode(texture.GetFirstMip(), platform);
    public static SKBitmap? Decode(this UTexture texture, ETexturePlatform platform = ETexturePlatform.DesktopMobile) => texture.Decode(texture.GetFirstMip(), platform);
    public static SKBitmap? Decode(this UTexture texture, FTexture2DMipMap? mip, ETexturePlatform platform = ETexturePlatform.DesktopMobile, int zLayer = 0)
    {
        if (texture.PlatformData is { FirstMipToSerialize: >= 0, VTData: { } vt } && vt.IsInitialized())
        {
            var tileSize = (int) vt.TileSize;
            var tileBorderSize = (int) vt.TileBorderSize;
            var tilePixelSize = (int) vt.GetPhysicalTileSize();
            var tileCrop = new SKRect(tileBorderSize, tileBorderSize, tilePixelSize - tileBorderSize, tilePixelSize - tileBorderSize);
            var level = texture.PlatformData.FirstMipToSerialize;

            FVirtualTextureTileOffsetData tileOffsetData;
            if (vt.IsLegacyData())
            {
                // calculate the max address in this mip
                // aka get the next mip max address and subtract it by the current mip max address
                var blockWidthInTiles = vt.GetWidthInTiles();
                var blockHeightInTiles = vt.GetHeightInTiles();
                var maxAddress = vt.TileIndexPerMip[Math.Min(level + 1, vt.NumMips)];
                tileOffsetData = new FVirtualTextureTileOffsetData(blockWidthInTiles, blockHeightInTiles, Math.Max(maxAddress - vt.TileIndexPerMip[level], 1));
            }
            else tileOffsetData = vt.TileOffsetData[level];

            var bitmapWidth = (int) tileOffsetData.Width * tileSize;
            var bitmapHeight = (int) tileOffsetData.Height * tileSize;
            var maxLevel = Math.Ceiling(Math.Log2(Math.Max(tileOffsetData.Width, tileOffsetData.Height)));
            if (maxLevel == 0 || vt.IsLegacyData())
            {
                // if we are here that means the mip is tiled and so the bitmap size must be lowered by one-fourth
                // if texture is legacy we must always lower the bitmap size because GetXXXXInTiles gives the number of tiles in mip 0
                // but that doesn't mean the mip is tiled in the first place
                var baseLevel = vt.IsLegacyData() ? maxLevel : Math.Ceiling(Math.Log2(Math.Max(vt.TileOffsetData[0].Width, vt.TileOffsetData[0].Height)));
                var factor = Convert.ToInt32(Math.Max(Math.Pow(2, vt.IsLegacyData() ? level : level - baseLevel), 1));
                bitmapWidth /= factor;
                bitmapHeight /= factor;
            }
            var bitmap = new SKBitmap(bitmapWidth, bitmapHeight, SKImageInfo.PlatformColorType, SKAlphaType.Unpremul);
            using var c = new SKCanvas(bitmap);

            for (uint layer = 0; layer < vt.NumLayers; layer++)
            {
                var layerFormat = vt.LayerTypes[layer];
                if (PixelFormatUtils.PixelFormats.ElementAtOrDefault((int) layerFormat) is not { Supported: true } formatInfo || formatInfo.BlockBytes == 0)
                    throw new NotImplementedException($"The supplied pixel format {layerFormat} is not supported!");

                var tileWidthInBlocks = tilePixelSize.DivideAndRoundUp(formatInfo.BlockSizeX);
                var tileHeightInBlocks = tilePixelSize.DivideAndRoundUp(formatInfo.BlockSizeY);
                var packedStride = tileWidthInBlocks * formatInfo.BlockBytes;
                var packedOutputSize = packedStride * tileHeightInBlocks;

                var layerData = _shared.Rent(packedOutputSize);
                for (uint tileIndexInMip = 0; tileIndexInMip < tileOffsetData.MaxAddress; tileIndexInMip++)
                {
                    if (!vt.IsValidAddress(level, tileIndexInMip)) continue;

                    var tileX = MathUtils.ReverseMortonCode2(tileIndexInMip);
                    var tileY = MathUtils.ReverseMortonCode2(tileIndexInMip >> 1);
                    var (chunkIndex, tileStart, tileLength) = vt.GetTileData(level, tileIndexInMip, layer);

                    switch (vt.Chunks[chunkIndex].CodecType[layer])
                    {
                        case EVirtualTextureCodec.ZippedGPU_DEPRECATED:
                            Compression.Decompress(vt.Chunks[chunkIndex].BulkData.Data, (int) tileStart, (int) tileLength, layerData, 0, packedOutputSize, CompressionMethod.Zlib);
                            break;
                        default:
                            Array.Copy(vt.Chunks[chunkIndex].BulkData.Data, tileStart, layerData, 0, packedOutputSize);
                            break;
                    }

                    DecodeBytes(layerData, tilePixelSize, tilePixelSize, 1, formatInfo, texture.IsNormalMap, out var data, out var colorType);

                    var (x, y) = (tileX * tileSize, tileY * tileSize);
                    var b = InstallPixels(data, new SKImageInfo(tilePixelSize, tilePixelSize, colorType, SKAlphaType.Unpremul));
                    c.DrawBitmap(b, tileCrop, new SKRect(x, y, x + tileSize, y + tileSize));
                    b.Dispose();
                }
                _shared.Return(layerData);
            }

            return bitmap;
        }

        if (mip != null)
        {
            var sizeX = mip.SizeX;
            var sizeY = mip.SizeY;

            if (texture.Format == EPixelFormat.PF_BC7)
            {
                sizeX = (sizeX + 3) / 4 * 4;
                sizeY = (sizeY + 3) / 4 * 4;
            }

            DecodeTexture(mip, sizeX, sizeY, mip.SizeZ, texture.Format, texture.IsNormalMap, platform, out var data, out var colorType);

            var offset = sizeX * sizeY * 4;
            var startIndex = offset * zLayer;
            var endIndex = startIndex + offset;
            return InstallPixels(data[startIndex..endIndex], new SKImageInfo(sizeX, sizeY, colorType, SKAlphaType.Unpremul));
        }

        return null;
    }

    public static List<SKBitmap>? DecodeTextureArray(this UTexture2DArray texture, ETexturePlatform platform = ETexturePlatform.DesktopMobile)
    {
        var mip = texture.GetFirstMip();

        if (mip is null) return null;
        
        var sizeX = mip.SizeX;
        var sizeY = mip.SizeY;

        if (texture.Format == EPixelFormat.PF_BC7)
        {
            sizeX = (sizeX + 3) / 4 * 4;
            sizeY = (sizeY + 3) / 4 * 4;
        }

        DecodeTexture(mip, sizeX, sizeY, mip.SizeZ, texture.Format, texture.IsNormalMap, platform, out var data,
            out var colorType);

        var bitmaps = new List<SKBitmap>();
        var offset = sizeX * sizeY * 4;
        for (var i = 0; i < mip.SizeZ; i++)
        {
            var startIndex = offset * i;
            var endIndex = startIndex + offset;
            if (endIndex > data.Length) break;
            bitmaps.Add(InstallPixels(data[startIndex..endIndex],
                new SKImageInfo(sizeX, sizeY, colorType, SKAlphaType.Unpremul)));
        }

        return bitmaps;

    }

    public static void DecodeTexture(FTexture2DMipMap? mip, int sizeX, int sizeY, int sizeZ, EPixelFormat format, bool isNormalMap, ETexturePlatform platform, out byte[] data, out SKColorType colorType)
    {
        if (mip?.BulkData.Data is not { Length: > 0 }) throw new ParserException("Supplied MipMap is null or has empty data!");
        if (PixelFormatUtils.PixelFormats.ElementAtOrDefault((int) format) is not { Supported: true } formatInfo || formatInfo.BlockBytes == 0) throw new NotImplementedException($"The supplied pixel format {format} is not supported!");

        var isXBPS = platform == ETexturePlatform.XboxAndPlaystation;
        var isNX = platform == ETexturePlatform.NintendoSwitch;

        // If the platform requires deswizzling, check if we should even try.
        if (isXBPS || isNX)
        {
            var blockSizeX = mip.SizeX / formatInfo.BlockSizeX;
            var blockSizeY = mip.SizeY / formatInfo.BlockSizeY;
            var totalBlocks = mip.BulkData.Data.Length / formatInfo.BlockBytes;
            if (blockSizeX * blockSizeY > totalBlocks) throw new ParserException("The supplied MipMap could not be untiled!");
        }

        var bytes = mip.BulkData.Data;

        // Handle deswizzling if necessary.
        if (isXBPS) bytes = PlatformDeswizzlers.DeswizzleXBPS(bytes, mip, formatInfo);
        else if (isNX) bytes = PlatformDeswizzlers.GetDeswizzledData(bytes, mip, formatInfo);

        DecodeBytes(bytes, sizeX, sizeY, sizeZ, formatInfo, isNormalMap, out data, out colorType);
    }

    private static void DecodeBytes(byte[] bytes, int sizeX, int sizeY, int sizeZ, FPixelFormatInfo formatInfo, bool isNormalMap, out byte[] data, out SKColorType colorType)
    {
        switch (formatInfo.UnrealFormat)
        {
            case EPixelFormat.PF_DXT1:
            {
                data = DXTDecoder.DXT1(bytes, sizeX, sizeY, sizeZ);
                colorType = SKColorType.Rgba8888;
                break;
            }
            case EPixelFormat.PF_DXT5:
                data = DXTDecoder.DXT5(bytes, sizeX, sizeY, sizeZ);
                colorType = SKColorType.Rgba8888;
                break;
            case EPixelFormat.PF_ASTC_4x4:
            case EPixelFormat.PF_ASTC_6x6:
            case EPixelFormat.PF_ASTC_8x8:
            case EPixelFormat.PF_ASTC_10x10:
            case EPixelFormat.PF_ASTC_12x12:
                data = ASTCDecoder.RGBA8888(
                    bytes,
                    formatInfo.BlockSizeX,
                    formatInfo.BlockSizeY,
                    formatInfo.BlockSizeZ,
                    sizeX, sizeY, sizeZ);
                colorType = SKColorType.Rgba8888;

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
                data = BCDecoder.BC4(bytes, sizeX, sizeY);
                colorType = SKColorType.Rgb888x;
                break;
            case EPixelFormat.PF_BC5:
                data = BCDecoder.BC5(bytes, sizeX, sizeY);
                colorType = SKColorType.Rgb888x;
                break;
            case EPixelFormat.PF_BC6H:
                // BC6H doesn't work no matter the pixel format, the closest we can get is either
                // Rgb565 DETEX_PIXEL_FORMAT_FLOAT_RGBX16 or Rgb565 DETEX_PIXEL_FORMAT_FLOAT_BGRX16

                data = Detex.DecodeDetexLinear(bytes, sizeX, sizeY, true,
                    DetexTextureFormat.DETEX_TEXTURE_FORMAT_BPTC_FLOAT,
                    DetexPixelFormat.DETEX_PIXEL_FORMAT_FLOAT_RGBX16);
                colorType = SKColorType.Rgb565;
                break;
            case EPixelFormat.PF_BC7:
                data = Detex.DecodeDetexLinear(bytes, sizeX, sizeY, false,
                    DetexTextureFormat.DETEX_TEXTURE_FORMAT_BPTC,
                    DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                colorType = SKColorType.Rgba8888;
                break;
            case EPixelFormat.PF_ETC1:
                data = Detex.DecodeDetexLinear(bytes, sizeX, sizeY, false,
                    DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC1,
                    DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                colorType = SKColorType.Rgba8888;
                break;
            case EPixelFormat.PF_ETC2_RGB:
                data = Detex.DecodeDetexLinear(bytes, sizeX, sizeY, false,
                    DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC2,
                    DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                colorType = SKColorType.Rgba8888;
                break;
            case EPixelFormat.PF_ETC2_RGBA:
                data = Detex.DecodeDetexLinear(bytes, sizeX, sizeY, false,
                    DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC2_EAC,
                    DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                colorType = SKColorType.Rgba8888;
                break;
            case EPixelFormat.PF_R16F:
            case EPixelFormat.PF_R16F_FILTER:
            case EPixelFormat.PF_G16:
                unsafe
                {
                    fixed (byte* d = bytes)
                    {
                        data = ConvertRawR16DataToRGB888X(sizeX, sizeY, d, sizeX * 2); // 2 BPP
                    }
                }

                colorType = SKColorType.Rgb888x;
                break;
            case EPixelFormat.PF_B8G8R8A8:
                data = bytes;
                colorType = SKColorType.Bgra8888;
                break;
            case EPixelFormat.PF_G8:
                data = bytes;
                colorType = SKColorType.Gray8;
                break;
            case EPixelFormat.PF_FloatRGBA:
                unsafe
                {
                    fixed (byte* d = bytes)
                    {
                        data = ConvertRawR16G16B16A16FDataToRGBA8888(sizeX, sizeY, d, sizeX * 8, false); // 8 BPP
                    }
                }

                colorType = SKColorType.Rgba8888;
                break;
            default: throw new NotImplementedException($"Unknown pixel format: {formatInfo.UnrealFormat}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe byte[] ConvertRawR16DataToRGB888X(int width, int height, byte* inp, int srcPitch)
    {
        // e.g. shadow maps
        var ret = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            var srcPtr = (ushort*) (inp + y * srcPitch);
            var destPtr = y * width * 4;
            for (int x = 0; x < width; x++)
            {
                var value16 = *srcPtr++;
                var value = FColor.Requantize16to8(value16);

                ret[destPtr++] = value;
                ret[destPtr++] = value;
                ret[destPtr++] = value;
                ret[destPtr++] = 255;
            }
        }

        return ret;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe byte[] ConvertRawR16G16B16A16FDataToRGBA8888(int width, int height, byte* inp, int srcPitch, bool linearToGamma)
    {
        var ret = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            var srcPtr = (ushort*) (inp + y * srcPitch);
            var destPtr = y * width * 4;

            for (int x = 0; x < width; x++)
            {
                var color = new FLinearColor(
                    HalfToFloat(*srcPtr++),
                    HalfToFloat(*srcPtr++),
                    HalfToFloat(*srcPtr++),
                    HalfToFloat(*srcPtr++)
                ).ToFColor(linearToGamma);
                ret[destPtr++] = color.R;
                ret[destPtr++] = color.G;
                ret[destPtr++] = color.B;
                ret[destPtr++] = color.A;
            }
        }

        return ret;
    }

    private static SKBitmap InstallPixels(byte[] data, SKImageInfo info)
    {
        var bitmap = new SKBitmap();
        unsafe
        {
            var pixelsPtr = NativeMemory.Alloc((nuint) data.Length);
            fixed (byte* p = data)
            {
                Unsafe.CopyBlockUnaligned(pixelsPtr, p, (uint) data.Length);
            }

            bitmap.InstallPixels(info, new IntPtr(pixelsPtr), info.RowBytes, (address, _) => NativeMemory.Free(address.ToPointer()));
        }
        return bitmap;
    }
}
