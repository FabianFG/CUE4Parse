using System;
using System.Buffers;
using System.Linq;
using System.Runtime.InteropServices;
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
            return DecodeVT(texture, vt);

        if (mip == null)
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
        return new CTexture( sizeX, sizeY, colorType, data);

    }

    private static unsafe Span<byte> GetSliceData(byte* data, int sizeX, int sizeY, int bytesPerPixel, int zLayer = 0)
    {
        var offset = sizeX * sizeY * bytesPerPixel;
        var startIndex = offset * zLayer;
        return new Span<byte>(data + startIndex, offset);
    }

    private static CTexture DecodeVT(UTexture texture, FVirtualTextureBuiltData vt)
    {
        unsafe
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
                tileOffsetData = vt.TileOffsetData[level];


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

            EPixelFormat colorType = EPixelFormat.PF_Unknown;
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
                        Compression.Decompress(vt.Chunks[chunkIndex].BulkData.Data!, (int)tileStart, (int)tileLength, layerData, 0, packedOutputSize, CompressionMethod.Zlib);

                    else
                        Array.Copy(vt.Chunks[chunkIndex].BulkData.Data!, tileStart, layerData, 0, packedOutputSize);

                    DecodeBytes(layerData, tilePixelSize, tilePixelSize, 1, formatInfo, texture.IsNormalMap, out var data, out var tileColorType);

                    if (pixelDataPtr is null)
                    {
                        colorType = tileColorType;
                        var tempFormatInfo = PixelFormatUtils.PixelFormats.ElementAtOrDefault((int) tileColorType)!;
                        bytesPerPixel = tempFormatInfo.BlockBytes / (tempFormatInfo.BlockSizeX * tempFormatInfo.BlockSizeY * tempFormatInfo.BlockSizeZ);
                        rowBytes = bytesPerPixel * bitmapWidth;
                        tileRowBytes = tileSize * bytesPerPixel;
                        var imageBytes = bitmapHeight * bitmapWidth * bytesPerPixel;
                        pixelDataPtr = NativeMemory.Alloc((nuint)imageBytes);
                        result = new Span<byte>(pixelDataPtr, imageBytes);
                    }
                    else if (colorType != tileColorType)
                        throw new NotSupportedException("multiple pixelformats/colortypes in a single virtual image is not supported");

                    for (int i = 0; i < tileSize; i++)
                    {
                        var tileOffset = ((i + tileBorderSize) * tilePixelSize + tileBorderSize) * bytesPerPixel;
                        var offset = tileX * bytesPerPixel + (tileY + i) * rowBytes;
                        var srcSpan = data.AsSpan(tileOffset, tileRowBytes);
                        var destSpan = result.Slice(offset);
                        srcSpan.CopyTo(destSpan);
                    }
                }

                ArrayPool<byte>.Shared.Return(layerData);
            }

            return new CTexture(bitmapWidth, bitmapHeight, colorType, GetSliceData((byte*)pixelDataPtr, bitmapWidth, bitmapHeight, bytesPerPixel).ToArray());
        }
    }

    public static unsafe CTexture[]? DecodeTextureArray(this UTexture2DArray texture, ETexturePlatform platform = ETexturePlatform.DesktopMobile)
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
            // sizeZ = sizeZ.Align(4);
        }

        DecodeTexture(mip, sizeX, sizeY, sizeZ, texture.Format, texture.IsNormalMap, platform, out var data, out var colorType);

        var bitmaps = new CTexture[sizeZ];
        var offset = sizeX * sizeY * 4;

        fixed (byte* dataPtr = data)
        {
            for (var i = 0; i < sizeZ; i++)
            {
                if (offset * (i + 1) > data.Length)
                    break;
                bitmaps[i] = new CTexture(sizeX, sizeY, colorType, GetSliceData(dataPtr, sizeX, sizeY, 4, i).ToArray());
            }
        }
        return bitmaps;
    }

    private static void DecodeTexture(FTexture2DMipMap? mip, int sizeX, int sizeY, int sizeZ, EPixelFormat format, bool isNormalMap, ETexturePlatform platform, out byte[] data, out EPixelFormat colorType)
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

    private static void DecodeBytes(byte[] bytes, int sizeX, int sizeY, int sizeZ, FPixelFormatInfo formatInfo, bool isNormalMap, out byte[] data, out EPixelFormat colorType)
    {
        //default return the original data and Format
        data = bytes;
        colorType = formatInfo.UnrealFormat;

        switch (formatInfo.UnrealFormat)
        {
            case EPixelFormat.PF_DXT1:
            {
                if (UseAssetRipperTextureDecoder)
                {
                    Bc1.Decompress(bytes, sizeX, sizeY, out data);
                    colorType = EPixelFormat.PF_B8G8R8A8;
                }
                else
                {
                    data = DXTDecoder.DXT1(bytes, sizeX, sizeY, sizeZ);
                    colorType = EPixelFormat.PF_R8G8B8A8;
                }
                break;
            }
            case EPixelFormat.PF_DXT5:
                if (UseAssetRipperTextureDecoder)
                {
                    Bc3.Decompress(bytes, sizeX, sizeY, out data);
                    colorType = EPixelFormat.PF_B8G8R8A8;
                }
                else
                {
                    data = DXTDecoder.DXT5(bytes, sizeX, sizeY, sizeZ);
                    colorType = EPixelFormat.PF_R8G8B8A8;
                }
                break;
            case EPixelFormat.PF_ASTC_4x4:
            case EPixelFormat.PF_ASTC_6x6:
            case EPixelFormat.PF_ASTC_8x8:
            case EPixelFormat.PF_ASTC_10x10:
            case EPixelFormat.PF_ASTC_12x12:
                data = ASTCDecoder.RGBA8888(bytes, formatInfo.BlockSizeX, formatInfo.BlockSizeY, formatInfo.BlockSizeZ, sizeX, sizeY, sizeZ);
                colorType = EPixelFormat.PF_R8G8B8A8;

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
                    Bc4.Decompress(bytes, sizeX, sizeY, out data);
                else
                    data = BCDecoder.BC4(bytes, sizeX, sizeY, sizeZ);
                colorType = EPixelFormat.PF_B8G8R8A8;
                break;
            case EPixelFormat.PF_BC5:
                if (UseAssetRipperTextureDecoder)
                    Bc5.Decompress(bytes, sizeX, sizeY, out data);
                else
                    data = BCDecoder.BC5(bytes, sizeX, sizeY, sizeZ);
                for (var i = 0; i < sizeX * sizeY; i++)
                    data[i * 4] = BCDecoder.GetZNormal(data[i * 4 + 2], data[i * 4 + 1]);
                colorType = EPixelFormat.PF_B8G8R8A8;
                break;
            case EPixelFormat.PF_BC6H:
                if (UseAssetRipperTextureDecoder)
                {
                    Bc6h.Decompress(bytes, sizeX, sizeY, false, out data);
                    colorType = EPixelFormat.PF_B8G8R8A8;
                }
                else
                {
                    // BC6H doesn't work no matter the pixel format, the closest we can get is either
                    // Rgb565 DETEX_PIXEL_FORMAT_FLOAT_RGBX16 or Rgb565 DETEX_PIXEL_FORMAT_FLOAT_BGRX16
                    data = DetexHelper.DecodeDetexLinear(bytes, sizeX, sizeY, true, DetexTextureFormat.DETEX_TEXTURE_FORMAT_BPTC_FLOAT, DetexPixelFormat.DETEX_PIXEL_FORMAT_FLOAT_RGBX16);
                    colorType = EPixelFormat.PF_FloatRGBA; //TODO idk
                }
                break;
            case EPixelFormat.PF_BC7:
                if (UseAssetRipperTextureDecoder)
                    Bc7.Decompress(bytes, sizeX, sizeY, out data);
                else
                    data = DetexHelper.DecodeDetexLinear(bytes, sizeX, sizeY * sizeZ, false, DetexTextureFormat.DETEX_TEXTURE_FORMAT_BPTC, DetexPixelFormat.DETEX_PIXEL_FORMAT_BGRA8);
                colorType = EPixelFormat.PF_B8G8R8A8;
                break;
            case EPixelFormat.PF_ETC1:
                data = DetexHelper.DecodeDetexLinear(bytes, sizeX, sizeY, false, DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC1, DetexPixelFormat.DETEX_PIXEL_FORMAT_BGRA8);
                colorType = EPixelFormat.PF_B8G8R8A8;
                break;
            case EPixelFormat.PF_ETC2_RGB:
                data = DetexHelper.DecodeDetexLinear(bytes, sizeX, sizeY, false, DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC2, DetexPixelFormat.DETEX_PIXEL_FORMAT_BGRA8);
                colorType = EPixelFormat.PF_B8G8R8A8;
                break;
            case EPixelFormat.PF_ETC2_RGBA:
                data = DetexHelper.DecodeDetexLinear(bytes, sizeX, sizeY, false, DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC2_EAC, DetexPixelFormat.DETEX_PIXEL_FORMAT_BGRA8);
                colorType = EPixelFormat.PF_B8G8R8A8;
                break;

            //SECTION: raw formats. Do nothing, we return original format and data
            case EPixelFormat.PF_B8G8R8A8:
            case EPixelFormat.PF_G8:
            case EPixelFormat.PF_A32B32G32R32F:
            case EPixelFormat.PF_FloatRGB:
            case EPixelFormat.PF_FloatRGBA:
            case EPixelFormat.PF_R32_FLOAT:
            case EPixelFormat.PF_G16R16F:
            case EPixelFormat.PF_G16R16:
            case EPixelFormat.PF_G32R32F:
            case EPixelFormat.PF_A16B16G16R16:
            case EPixelFormat.PF_R16F:
            case EPixelFormat.PF_G16:
            case EPixelFormat.PF_R32G32B32F:
                break;

            case EPixelFormat.PF_R16F_FILTER:
                colorType = EPixelFormat.PF_R16F;
                break;
            case EPixelFormat.PF_G16R16F_FILTER:
                colorType = EPixelFormat.PF_G16R16F;
                break;

            default:
                throw new NotImplementedException($"Unknown pixel format: {formatInfo.UnrealFormat}");
        }
    }
}
