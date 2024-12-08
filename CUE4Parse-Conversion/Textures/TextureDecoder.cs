using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AssetRipper.TextureDecoder.Bc;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.Utils;

using CUE4Parse_Conversion.Textures.ASTC;
using CUE4Parse_Conversion.Textures.BC;
using CUE4Parse_Conversion.Textures.DXT;

using SkiaSharp;

using static CUE4Parse.Utils.TypeConversionUtils;

namespace CUE4Parse_Conversion.Textures;

public static class TextureDecoder
{
    public static bool UseAssetRipperTextureDecoder = false;

    public static SKBitmap? Decode(this UTexture2D texture, int maxMipSize, ETexturePlatform platform = ETexturePlatform.DesktopMobile) => texture.Decode(texture.GetMipByMaxSize(maxMipSize), platform);
    public static SKBitmap? Decode(this UTexture2D texture, ETexturePlatform platform = ETexturePlatform.DesktopMobile) => texture.Decode(texture.GetFirstMip(), platform);
    public static SKBitmap? Decode(this UTexture texture, ETexturePlatform platform = ETexturePlatform.DesktopMobile) => texture.Decode(texture.GetFirstMip(), platform);
    public static unsafe SKBitmap? Decode(this UTexture texture, FTexture2DMipMap? mip, ETexturePlatform platform = ETexturePlatform.DesktopMobile, int zLayer = 0)
    {
        if (texture.PlatformData is { FirstMipToSerialize: >= 0, VTData: { } vt } && vt.IsInitialized())
        {
            var tileSize = (int) vt.TileSize;
            var tileBorderSize = (int) vt.TileBorderSize;
            var tilePixelSize = (int) vt.GetPhysicalTileSize();
            const int level = 0;

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
            else
            {
                tileOffsetData = vt.TileOffsetData[level];
            }

            var bitmapWidth = (int) tileOffsetData.Width * tileSize;
            var bitmapHeight = (int) tileOffsetData.Height * tileSize;
            var maxLevel = Math.Ceiling(Math.Log2(Math.Max(tileOffsetData.Width, tileOffsetData.Height)));
            if (tileOffsetData.MaxAddress > 1 && (maxLevel == 0 || vt.IsLegacyData()))
            {
                // if we are here that means the mip is tiled and so the bitmap size must be lowered by one-fourth
                // if texture is legacy we must always lower the bitmap size because GetXXXXInTiles gives the number of tiles in mip 0
                // but that doesn't mean the mip is tiled in the first place
                var baseLevel = vt.IsLegacyData() ? maxLevel : Math.Ceiling(Math.Log2(Math.Max(vt.TileOffsetData[0].Width, vt.TileOffsetData[0].Height)));
                var factor = Convert.ToInt32(Math.Max(Math.Pow(2, vt.IsLegacyData() ? level : level - baseLevel), 1));
                bitmapWidth /= factor;
                bitmapHeight /= factor;
            }

            var colorType = SKColorType.Unknown;
            void* pixelDataPtr = null;
            var bytesPerPixel = 0;
            var rowBytes = 0;
            var tileRowBytes = 0;
            var result = Span<byte>.Empty;

            for (uint layer = 0; layer < vt.NumLayers; layer++)
            {
                var layerFormat = vt.LayerTypes[layer];
                if (PixelFormatUtils.PixelFormats.ElementAtOrDefault((int) layerFormat) is not { Supported: true } formatInfo || formatInfo.BlockBytes == 0)
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
                        Compression.Decompress(vt.Chunks[chunkIndex].BulkData.Data!, (int)tileStart, (int)tileLength,
                            layerData, 0, packedOutputSize, CompressionMethod.Zlib);
                    }
                    else
                    {
                        Array.Copy(vt.Chunks[chunkIndex].BulkData.Data!, tileStart, layerData, 0, packedOutputSize);
                    }

                    DecodeBytes(layerData, tilePixelSize, tilePixelSize, 1, formatInfo, texture.IsNormalMap, out var data, out var tileColorType);

                    if (pixelDataPtr is null)
                    {
                        colorType = tileColorType;
                        var tempInfo = new SKImageInfo(bitmapWidth, bitmapHeight, colorType, SKAlphaType.Unpremul);
                        bytesPerPixel = tempInfo.BytesPerPixel;
                        rowBytes = tempInfo.RowBytes;
                        tileRowBytes = tileSize * bytesPerPixel;
                        var imageBytes = tempInfo.BytesSize;
                        pixelDataPtr = NativeMemory.Alloc((nuint)imageBytes);
                        result = new Span<byte>(pixelDataPtr, imageBytes);
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
                        var destSpan = result.Slice(offset);
                        srcSpan.CopyTo(destSpan);
                    }

                    TestSaveTile(tilePixelSize, tileColorType, tileIndexInMip, data);
                }

                ArrayPool<byte>.Shared.Return(layerData);
            }

            var bitmap = new SKBitmap();
            var imageInfo = new SKImageInfo(bitmapWidth, bitmapHeight, colorType, SKAlphaType.Unpremul);
            bitmap.InstallPixels(imageInfo, (nint)pixelDataPtr, rowBytes,
                (bmpPixelAddr, _) => NativeMemory.Free(bmpPixelAddr.ToPointer()));
            return bitmap;
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

            return InstallPixels(GetImageDataRange(data, mip, sizeX, sizeY, zLayer), new SKImageInfo(sizeX, sizeY, colorType, SKAlphaType.Unpremul));
        }

        return null;
    }

    private static unsafe void TestSaveTile(int tilePixelSize, SKColorType tileColorType, uint tileIndexInMip, Span<byte> pixelData)
    {
#if TEST_SAVE_TILES
        using var tempBmp = new SKBitmap();
        var tempImageInfo = new SKImageInfo(tilePixelSize, tilePixelSize, tileColorType, SKAlphaType.Unpremul);
        var tempPixelBuffer = NativeMemory.Alloc((nuint)pixelData.Length);
        var tempPixelSpan = new Span<byte>(tempPixelBuffer, pixelData.Length);
        pixelData.CopyTo(tempPixelSpan);
        tempBmp.InstallPixels(tempImageInfo, (nint)tempPixelBuffer, tempImageInfo.RowBytes,
            (bmpPixelAddr, _) => NativeMemory.Free(bmpPixelAddr.ToPointer()));
        using var tempData = tempBmp.Encode(ETextureFormat.Png, 100);
        using var tempDataStream = tempData.AsStream(true);
        var downloadsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var testDir = Directory.CreateDirectory(Path.Combine(downloadsDir, "CUE4ParseTest"));
        using var tempFs = File.Create(Path.Combine(testDir.FullName, $"test_tile_{tileIndexInMip}.png"));
        tempDataStream.CopyTo(tempFs);
#endif
    }

    public static SKBitmap[]? DecodeTextureArray(this UTexture2DArray texture, ETexturePlatform platform = ETexturePlatform.DesktopMobile)
    {
        var mip = texture.GetFirstMip();

        if (mip is null) return null;

        var sizeX = mip.SizeX;
        var sizeY = mip.SizeY;
        var sizeZ = mip.SizeZ;

        if (texture.Format == EPixelFormat.PF_BC7)
        {
            sizeX = sizeX.Align(4);
            sizeY = sizeY.Align(4);
            sizeZ = sizeZ.Align(4);
        }

        DecodeTexture(mip, sizeX, sizeY, sizeZ, texture.Format, texture.IsNormalMap, platform, out var data,
            out var colorType);

        var bitmaps = new List<SKBitmap>();
        var offset = sizeX * sizeY * 4;
        for (var i = 0; i < sizeZ; i++)
        {
            if (offset * (i + 1) > data.Length) break;
            bitmaps.Add(InstallPixels(GetImageDataRange(data, mip, sizeX, sizeY, i),
                new SKImageInfo(sizeX, sizeY, colorType, SKAlphaType.Unpremul)));
        }

        return bitmaps.ToArray();
    }

    private static byte[] GetImageDataRange(byte[] data, FTexture2DMipMap mip, int sizeX, int sizeY, int zLayer)
    {
        var offset = sizeX * sizeY * 4;
        var startIndex = offset * zLayer;
        var endIndex = startIndex + offset;

        return endIndex > data.Length ? data : data[startIndex..endIndex];
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
                if (UseAssetRipperTextureDecoder)
                {
                    Bc1.Decompress(bytes, sizeX, sizeY, out data);
                    colorType = SKColorType.Bgra8888;
                }
                else
                {
                    data = DXTDecoder.DXT1(bytes, sizeX, sizeY, sizeZ);
                    colorType = SKColorType.Rgba8888;
                }
                break;
            }
            case EPixelFormat.PF_DXT5:
                if (UseAssetRipperTextureDecoder)
                {
                    Bc3.Decompress(bytes, sizeX, sizeY, out data);
                    colorType = SKColorType.Bgra8888;
                }
                else
                {
                    data = DXTDecoder.DXT5(bytes, sizeX, sizeY, sizeZ);
                    colorType = SKColorType.Rgba8888;
                }
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
                if (UseAssetRipperTextureDecoder)
                {
                    Bc4.Decompress(bytes, sizeX, sizeY, out data);
                    colorType = SKColorType.Bgra8888;
                }
                else
                {
                    data = BCDecoder.BC4(bytes, sizeX, sizeY, sizeZ);
                    colorType = SKColorType.Rgb888x;
                }
                break;
            case EPixelFormat.PF_BC5:
                if (UseAssetRipperTextureDecoder)
                {
                    Bc5.Decompress(bytes, sizeX, sizeY, out data);
                    colorType = SKColorType.Bgra8888;
                }
                else
                {
                    data = BCDecoder.BC5(bytes, sizeX, sizeY, sizeZ);
                    colorType = SKColorType.Rgb888x;
                }
                break;
            case EPixelFormat.PF_BC6H:
                if (UseAssetRipperTextureDecoder)
                {
                    Bc6h.Decompress(bytes, sizeX, sizeY, false, out data);
                    colorType = SKColorType.Bgra8888;
                }
                else
                {
                    // BC6H doesn't work no matter the pixel format, the closest we can get is either
                    // Rgb565 DETEX_PIXEL_FORMAT_FLOAT_RGBX16 or Rgb565 DETEX_PIXEL_FORMAT_FLOAT_BGRX16
                    data = Detex.DecodeDetexLinear(bytes, sizeX, sizeY, true,
                        DetexTextureFormat.DETEX_TEXTURE_FORMAT_BPTC_FLOAT,
                        DetexPixelFormat.DETEX_PIXEL_FORMAT_FLOAT_RGBX16);
                    colorType = SKColorType.Rgb565;
                }
                break;
            case EPixelFormat.PF_BC7:
                if (UseAssetRipperTextureDecoder)
                {
                    Bc7.Decompress(bytes, sizeX, sizeY, out data);
                    colorType = SKColorType.Bgra8888;
                }
                else
                {
                    data = Detex.DecodeDetexLinear(bytes, sizeX, sizeY, false,
                        DetexTextureFormat.DETEX_TEXTURE_FORMAT_BPTC,
                        DetexPixelFormat.DETEX_PIXEL_FORMAT_RGBA8);
                    colorType = SKColorType.Rgba8888;
                }
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
                        data = ConvertRawR16G16B16A16FDataToRGBA8888(sizeX, sizeY, sizeZ, d, sizeX * 8, false); // 8 BPP
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
    private static unsafe byte[] ConvertRawR16G16B16A16FDataToRGBA8888(int width, int height, int depth, byte* inp, int srcPitch, bool linearToGamma)
    {
        var ret = new byte[width * height * depth * 4];
        for (var z = 0; z < depth; z++)
        {
            for (int y = 0; y < height; y++)
            {
                var srcPtr = (ushort*) (inp + z * height * srcPitch + y * srcPitch);
                var destPtr = z * height * width * 4 + y * width * 4;

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
