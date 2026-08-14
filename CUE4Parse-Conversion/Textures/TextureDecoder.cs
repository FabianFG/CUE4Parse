using System.Buffers;
using System.Runtime.InteropServices;
using AssetRipper.TextureDecoder.Astc;
using AssetRipper.TextureDecoder.Bc;
using AssetRipper.TextureDecoder.Etc;
using AssetRipper.TextureDecoder.Pvrtc;
using AssetRipper.TextureDecoder.Rgb.Formats;
using CUE4Parse_Conversion.Textures.ASTC;
using CUE4Parse_Conversion.Textures.BC;
using CUE4Parse_Conversion.Textures.ETC;
using CUE4Parse_Conversion.Textures.Crunch;
using CUE4Parse_Conversion.Textures.Custom;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse_Conversion.Textures;

public static class TextureDecoder
{
    public static bool UseAssetRipperTextureDecoder { get; set; } = false;
    internal static readonly bool IsWindows = OperatingSystem.IsWindows();

    public static CTexture? Decode(this UTexture texture, int maxMipSize, ETexturePlatform platform = ETexturePlatform.DesktopMobile) => texture.DecodeMip(texture.GetMipIndexByMaxSize(maxMipSize), platform);
    public static CTexture? Decode(this UTexture texture, ETexturePlatform platform = ETexturePlatform.DesktopMobile) => texture.DecodeMip(texture.GetFirstMipIndex(), platform);
    public static CTexture? Decode(this UTexture texture, FTexture2DMipMap? mip, ETexturePlatform platform = ETexturePlatform.DesktopMobile, int zLayer = 0)
    {
        if (texture.PlatformData is { FirstMipToSerialize: >= 0, VTData: { } vt } && vt.IsInitialized())
            return DecodeVT(texture, vt);

        if (mip is null) return null; // TODO: we should let it throw the exception

        DecodeTexture(texture, mip, platform, out var data, out var colorType, out var sizeX, out var sizeY, out var sizeZ);
        return new CTexture(sizeX, sizeY, colorType, data);
    }

    public static CTexture? DecodeMip(this UTexture texture, int mipIndex, ETexturePlatform platform = ETexturePlatform.DesktopMobile, int zLayer = 0)
    {
        if (texture.PlatformData is { FirstMipToSerialize: >= 0, VTData: { } vt } && vt.IsInitialized())
            return DecodeVT(texture, vt, mipIndex);

        var mip = texture.GetMip(mipIndex);
        if (mip is null) return null; // TODO: we should let it throw the exception

        DecodeTexture(texture, mip, platform, out var data, out var colorType, out var sizeX, out var sizeY, out var sizeZ);
        return new CTexture(sizeX, sizeY, colorType, data);
    }

    private static unsafe Span<byte> GetSliceData(byte* data, int sizeX, int sizeY, int bytesPerPixel, int zLayer = 0)
    {
        var offset = sizeX * sizeY * bytesPerPixel;
        var startIndex = offset * zLayer;
        return new Span<byte>(data + startIndex, offset);
    }

    public static int GetMinLevel(FVirtualTextureBuiltData vt)
    {
        for ( var i = 0; i < vt.NumMips; i++)
        {
            var tileOffsetData = vt.GetTileOffsetData(i);
            ulong length = tileOffsetData.Height * tileOffsetData.Width * vt.TileSize * vt.TileSize * 4; // for simplicity just use 4 bytes per pixel
            if (length < (ulong)Array.MaxLength)
                return i;
        }
        return 0;
    }

    private static CTexture DecodeVT(UTexture texture, FVirtualTextureBuiltData vt, int mip = -1)
    {
        unsafe
        {
            var tileSize = (int) vt.TileSize;
            var tileBorderSize = (int) vt.TileBorderSize;
            var tilePixelSize = (int) vt.GetPhysicalTileSize();
            var minLevel = GetMinLevel(vt);
            int level = mip <= -1 ? minLevel : Math.Max(mip, minLevel);

            var tileOffsetData = vt.GetTileOffsetData(level);

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
                if (!PixelFormatUtils.PixelFormats.TryGetValue(layerFormat, out var formatInfo) || !formatInfo.Supported || formatInfo.BlockBytes == 0)
                    throw new NotImplementedException($"The supplied pixel format {layerFormat} is not supported!");

                var tileWidthInBlocks = tilePixelSize.DivideAndRoundUp(formatInfo.BlockSizeX);
                var tileHeightInBlocks = tilePixelSize.DivideAndRoundUp(formatInfo.BlockSizeY);
                var packedStride = tileWidthInBlocks * formatInfo.BlockBytes;
                var packedOutputSize = packedStride * tileHeightInBlocks;

                var layerData = ArrayPool<byte>.Shared.Rent(packedOutputSize);
                var crunchContextCache = new Dictionary<(int ChunkIndex, uint Layer), CrunchDecoder.CrunchContext>();

                for (uint tileIndexInMip = 0; tileIndexInMip < tileOffsetData.MaxAddress; tileIndexInMip++)
                {
                    if (!vt.IsValidAddress(level, tileIndexInMip)) continue;

                    var tileX = (int)MathUtils.ReverseMortonCode2(tileIndexInMip) * tileSize;
                    var tileY = (int)MathUtils.ReverseMortonCode2(tileIndexInMip >> 1) * tileSize;
                    var (chunkIndex, tileStart, tileLength) = vt.GetTileData(level, tileIndexInMip, layer);

                    if (vt.Chunks[chunkIndex].CodecType[layer] == EVirtualTextureCodec.ZippedGPU_DEPRECATED)
                        Compression.Decompress(vt.Chunks[chunkIndex].BulkData.Data!, (int)tileStart, (int)tileLength, layerData, 0, packedOutputSize, CompressionMethod.Zlib);
                    else if (vt.Chunks[chunkIndex].CodecType[layer] == EVirtualTextureCodec.Crunch_DEPRECATED)
                    {
                        var chunk = vt.Chunks[chunkIndex];
                        var chunkData = chunk.BulkData.Data!;
                        var contextKey = (chunkIndex, layer);
                        try
                        {
                            if (!crunchContextCache.TryGetValue(contextKey, out var context))
                            {
                                var headerOffset = (int) chunk.CodecPayloadOffset[layer];
                                var headerEnd = (int) chunk.CodecPayloadSize;
                                if (layer + 1 < chunk.CodecPayloadOffset.Length)
                                {
                                    var nextOffset = (int) chunk.CodecPayloadOffset[layer + 1];
                                    if (nextOffset > headerOffset)
                                        headerEnd = nextOffset;
                                }

                                var headerSize = headerEnd - headerOffset;
                                if (headerSize <= 0 || (headerOffset + headerSize) > chunkData.Length)
                                    throw new ParserException("Incorrect crunch codec payload");

                                context = new CrunchDecoder.CrunchContext(chunkData, headerOffset, headerSize);
                                crunchContextCache[contextKey] = context;
                            }

                            var rowPitch = (uint) (tilePixelSize / formatInfo.BlockSizeX * formatInfo.BlockBytes);
                            if (!context.TryDecompressSegment(chunkData, checked((int) tileStart), tileLength, layerData, rowPitch, 0))
                                throw new ParserException($"Failed to unpack tile ({tileX}, {tileY}) at {tileStart}");
                        }
                        catch (ParserException e)
                        {
                            Log.Error(e, "Failed to decompress crunch codec texture");
                            break;
                        }
                    }
                    else
                        Array.Copy(vt.Chunks[chunkIndex].BulkData.Data!, tileStart, layerData, 0, packedOutputSize);

                    DecodeBytes(layerData, tilePixelSize, tilePixelSize, 1, formatInfo, texture.IsNormalMap, out var data, out var tileColorType);

                    if (pixelDataPtr is null)
                    {
                        colorType = tileColorType;
                        if (!PixelFormatUtils.PixelFormats.TryGetValue(tileColorType, out var tempFormatInfo))
                            throw new NotImplementedException("Unsupported pixel format: " + tileColorType);
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
                        var destSpan = result[offset..];
                        srcSpan.CopyTo(destSpan);
                    }
                }

                // free crunch context
                foreach (var context in crunchContextCache.Values)
                    context.Dispose();

                ArrayPool<byte>.Shared.Return(layerData);
            }
            var managedData = GetSliceData((byte*)pixelDataPtr, bitmapWidth, bitmapHeight, bytesPerPixel).ToArray();
            NativeMemory.Free(pixelDataPtr);

            return new CTexture(bitmapWidth, bitmapHeight, colorType, managedData);
        }
    }

    public static unsafe CTexture[]? DecodeTextureArray(this UTexture2DArray texture, ETexturePlatform platform = ETexturePlatform.DesktopMobile) => texture.DecodeTextureArray(texture.GetFirstMipIndex(), platform);
    public static unsafe CTexture[]? DecodeTextureArray(this UTexture2DArray texture, int mipIndex, ETexturePlatform platform = ETexturePlatform.DesktopMobile) => texture.DecodeTextureArray(texture.GetMip(mipIndex), platform);
    public static unsafe CTexture[]? DecodeTextureArray(this UTexture2DArray texture, FTexture2DMipMap? mip, ETexturePlatform platform = ETexturePlatform.DesktopMobile)
    {
        if (mip is null) return null; // TODO: we should let it throw the exception

        DecodeTexture(texture, mip, platform, out var data, out var colorType, out var sizeX, out var sizeY, out var sizeZ);

        var bitmaps = new CTexture[sizeZ];
        var bytesPerPixel = GetBytesPerPixel(colorType);
        var offset = sizeX * sizeY * bytesPerPixel;

        fixed (byte* dataPtr = data)
        {
            for (var i = 0; i < sizeZ; i++)
            {
                if (offset * (i + 1) > data.Length)
                    break;
                bitmaps[i] = new CTexture(sizeX, sizeY, colorType, GetSliceData(dataPtr, sizeX, sizeY, bytesPerPixel, i).ToArray());
            }
        }
        return bitmaps;
    }

    private static void DecodeTexture(UTexture texture, FTexture2DMipMap? mip, ETexturePlatform platform, out byte[] data, out EPixelFormat colorType, out int sizeX, out int sizeY, out int sizeZ)
    {
        var format = texture.Format;
        if (mip?.BulkData?.Data is not { Length: > 0 })
            throw new ParserException("Supplied MipMap is null or has empty data!");
        if (!PixelFormatUtils.PixelFormats.TryGetValue(format, out var formatInfo) || !formatInfo.Supported || formatInfo.BlockBytes == 0)
            throw new NotImplementedException($"The supplied pixel format {format} is not supported!");

        var bytes = mip.BulkData.Data;
        sizeX = mip.SizeX;
        sizeY = mip.SizeY;
        sizeZ = mip.SizeZ;

        if (texture is UVolumeTexture or UTextureCube)
        {
            var slices = texture.PlatformData.GetNumSlices();
            if (texture.Owner?.Provider?.Versions.Game == EGame.GAME_Borderlands4)
            {
                slices = slices != 1 ? slices >> 1 : 1;
            }

            // A volume's depth shrinks with every mip and is written on the mip itself, while
            // PackedData only describes mip 0 and doesn't always work with it. Mips below 4.20
            // have no depth at all, so we fall back to the slice count when there's nothing usable
            if (texture is UVolumeTexture && sizeZ > 1)
            {
                slices = sizeZ;
            }

            sizeY *= slices;
            if (sizeZ == slices) sizeZ = 1;
        }

        if (format == EPixelFormat.PF_BC7)
        {
            sizeX = sizeX.Align(4);
            sizeY = sizeY.Align(4);
        }

        // TODO: Only known game to use this is PUBG Mobile, not sure if this is right place to decompress, probably not and should be refactored
        if (texture.PlatformData.PixelFormat.EndsWith("_crunched", StringComparison.OrdinalIgnoreCase))
            bytes = CrunchDecoder.DecompressMip(bytes, sizeX, sizeY, sizeZ, formatInfo);

        // If the platform requires deswizzling, check if we should even try.
        if (platform is not ETexturePlatform.DesktopMobile)
        {
            var blockSizeX = mip.SizeX / formatInfo.BlockSizeX;
            var blockSizeY = mip.SizeY / formatInfo.BlockSizeY;
            var totalBlocks = bytes.Length / formatInfo.BlockBytes;
            if (blockSizeX * blockSizeY > totalBlocks)
                throw new ParserException("The supplied MipMap could not be untiled!");
        }

        // Handle deswizzling if necessary.
        switch (platform)
        {
            case ETexturePlatform.XboxAndPlaystation4:
                bytes = PlatformDeswizzlers.DeswizzleXBPS4(bytes, mip, formatInfo);
                break;
            case ETexturePlatform.NintendoSwitch:
                bytes = PlatformDeswizzlers.GetDeswizzledData(bytes, mip, formatInfo);
                break;
            case ETexturePlatform.Playstation5 when texture.CookPlatformTilingSettings is not ETextureCookPlatformTilingSettings.TCPTS_DoNotTile:
                bytes = PlatformDeswizzlers.DeswizzlePS5(bytes, mip, formatInfo);
                break;
        }

        DecodeBytes(bytes, sizeX, sizeY, sizeZ, formatInfo, texture.IsNormalMap, out data, out colorType);
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
                    Bc1.Decompress<ColorRGBA<byte>, byte>(bytes, sizeX, sizeY, out data);
                }
                else
                {
                    data = BCDecoder.BC1(bytes, sizeX, sizeY, sizeZ);
                }
                colorType = EPixelFormat.PF_R8G8B8A8;
                break;
            }
            case EPixelFormat.PF_DXT3:
            {
                if (UseAssetRipperTextureDecoder)
                {
                    Bc2.Decompress<ColorRGBA<byte>, byte>(bytes, sizeX, sizeY, out data);
                }
                else
                {
                    data = BCDecoder.BC2(bytes, sizeX, sizeY, sizeZ);
                    }
                colorType = EPixelFormat.PF_R8G8B8A8;
                break;
            }
            case EPixelFormat.PF_DXT5:
                if (UseAssetRipperTextureDecoder)
                {
                    Bc3.Decompress<ColorRGBA<byte>, byte>(bytes, sizeX, sizeY, out data);
                }
                else
                {
                    data = BCDecoder.BC3(bytes, sizeX, sizeY, sizeZ);
                }
                colorType = EPixelFormat.PF_R8G8B8A8;
                break;
            case EPixelFormat.PF_ASTC_4x4:
            case EPixelFormat.PF_ASTC_6x6:
            case EPixelFormat.PF_ASTC_8x8:
            case EPixelFormat.PF_ASTC_10x10:
            case EPixelFormat.PF_ASTC_12x12:
            case EPixelFormat.PF_ASTC_8x5:
            case EPixelFormat.PF_ASTC_8x6:
            case EPixelFormat.PF_ASTC_10x8:
                if (UseAssetRipperTextureDecoder)
                {
                    AstcDecoder.DecodeASTC<ColorRGBA<byte>, byte>(bytes, sizeX, sizeY, formatInfo.BlockSizeX, formatInfo.BlockSizeY, out data);
                }
                else
                {
                    data = ASTCDecoder.RGBA8888(bytes, formatInfo.BlockSizeX, formatInfo.BlockSizeY, formatInfo.BlockSizeZ, sizeX, sizeY, sizeZ);
                }
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
                    Bc4.Decompress<ColorBGRA<byte>, byte>(bytes, sizeX, sizeY * sizeZ, out data);
                else
                    data = BCDecoder.BC4(bytes, sizeX, sizeY, sizeZ);
                colorType = EPixelFormat.PF_B8G8R8A8;
                break;
            case EPixelFormat.PF_BC5:
                if (UseAssetRipperTextureDecoder)
                {
                    Bc5.Decompress<ColorBGRA<byte>, byte>(bytes, sizeX, sizeY * sizeZ, out data);
                    for (var i = 0; i < sizeX * sizeY * sizeZ; i++)
                        data[i * 4] = BCDecoder.GetZNormal(data[i * 4 + 2], data[i * 4 + 1]);
                }
                else
                {
                    // Blue channel is already restored in BCDecoder.BC5
                    data = BCDecoder.BC5(bytes, sizeX, sizeY, sizeZ);
                }
                colorType = EPixelFormat.PF_B8G8R8A8;
                break;
            case EPixelFormat.PF_BC6H:
                if (UseAssetRipperTextureDecoder)
                    Bc6h.Decompress<ColorRGBA<byte>, byte>(bytes, sizeX, sizeY, false, out data);
                else
                    data = BCDecoder.BC6H(bytes, sizeX, sizeY, sizeZ);
                colorType = EPixelFormat.PF_R8G8B8A8;
                break;
            case EPixelFormat.PF_BC6H_Signed:
                Bc6h.Decompress<ColorRGBA<byte>, byte>(bytes, sizeX, sizeY, true, out data);
                colorType = EPixelFormat.PF_R8G8B8A8;
                break;
            case EPixelFormat.PF_BC7:
                if (UseAssetRipperTextureDecoder || !IsWindows)
                {
                    Bc7.Decompress<ColorRGBA<byte>, byte>(bytes, sizeX, sizeY, out data);
                    colorType = EPixelFormat.PF_R8G8B8A8;
                }
                else
                {
                    data = DetexHelper.DecodeDetexLinear(bytes, sizeX, sizeY * sizeZ, false, DetexTextureFormat.DETEX_TEXTURE_FORMAT_BPTC, DetexPixelFormat.DETEX_PIXEL_FORMAT_BGRA8);
                    colorType = EPixelFormat.PF_B8G8R8A8;
                }
                break;
            case EPixelFormat.PF_ETC1:
                if (UseAssetRipperTextureDecoder || !IsWindows)
                {
                    EtcDecoder.DecompressETC<ColorRGBA<byte>, byte>(bytes, sizeX, sizeY, out data);
                    colorType = EPixelFormat.PF_R8G8B8A8;
                }
                else
                {
                    data = DetexHelper.DecodeDetexLinear(bytes, sizeX, sizeY, false, DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC1, DetexPixelFormat.DETEX_PIXEL_FORMAT_BGRA8);
                    colorType = EPixelFormat.PF_B8G8R8A8;
                }
                break;
            case EPixelFormat.PF_ETC2_RGB:
                if (UseAssetRipperTextureDecoder || !IsWindows)
                {
                    EtcDecoder.DecompressETC2<ColorRGBA<byte>, byte>(bytes, sizeX, sizeY, out data);
                    colorType = EPixelFormat.PF_R8G8B8A8;
                }
                else
                {
                    data = DetexHelper.DecodeDetexLinear(bytes, sizeX, sizeY, false, DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC2, DetexPixelFormat.DETEX_PIXEL_FORMAT_BGRA8);
                    colorType = EPixelFormat.PF_B8G8R8A8;
                }
                break;
            case EPixelFormat.PF_ETC2_RGBA:
                if (UseAssetRipperTextureDecoder || !IsWindows)
                {
                    EtcDecoder.DecompressETC2A8<ColorRGBA<byte>, byte>(bytes, sizeX, sizeY, out data);
                    colorType = EPixelFormat.PF_R8G8B8A8;
                }
                else
                {
                    data = DetexHelper.DecodeDetexLinear(bytes, sizeX, sizeY, false, DetexTextureFormat.DETEX_TEXTURE_FORMAT_ETC2_EAC, DetexPixelFormat.DETEX_PIXEL_FORMAT_BGRA8);
                    colorType = EPixelFormat.PF_B8G8R8A8;
                }
                break;
            case EPixelFormat.PF_ETC2_R11:
            case EPixelFormat.PF_ETC2_R11_EAC:
                if (UseAssetRipperTextureDecoder)
                {
                    EtcDecoder.DecompressEACRUnsigned<ColorRGBA<byte>, byte>(bytes, sizeX, sizeY, out data);
                    colorType = EPixelFormat.PF_R8G8B8A8;
                }
                else
                {
                    data = EacDecoder.DecodeR11(bytes, sizeX, sizeY);
                    colorType = EPixelFormat.PF_B8G8R8A8;
                }
                break;
            case EPixelFormat.PF_ETC2_RG11_EAC:
                EtcDecoder.DecompressEACRGUnsigned<ColorRGBA<byte>, byte>(bytes, sizeX, sizeY, out data);
                colorType = EPixelFormat.PF_R8G8B8A8;
                break;
            // Uses AssetRipper since depth data doesn't exist
            // If this format is used in any UE4/UE5, then switch to the different decoder
            case EPixelFormat.PF_PVRTC2:
                PvrtcDecoder.DecompressPVRTC<ColorRGBA<byte>, byte>(bytes, sizeX, sizeY, true, out data);
                colorType = EPixelFormat.PF_R8G8B8A8;
                break;
            case EPixelFormat.PF_PVRTC4:
                PvrtcDecoder.DecompressPVRTC<ColorRGBA<byte>, byte>(bytes, sizeX, sizeY, false, out data);
                colorType = EPixelFormat.PF_R8G8B8A8;
                break;
            case EPixelFormat.PF_B4G4R4A4:
                data = CustomFormatDecoder.B4G4R4A4(bytes, sizeX, sizeY, sizeZ);
                colorType = EPixelFormat.PF_B8G8R8A8;
                break;

            //SECTION: raw formats. Do nothing, we return original format and data
            case EPixelFormat.PF_A8R8G8B8:
            case EPixelFormat.PF_B8G8R8A8:
            case EPixelFormat.PF_V8U8:
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

    private static int GetBytesPerPixel(EPixelFormat pixelFormat)
    {
        var formatKvp = PixelFormatUtils.PixelFormats.ElementAtOrDefault((int) pixelFormat)!;
        var formatInfo = formatKvp.Value;
        return formatInfo.BlockBytes / (formatInfo.BlockSizeX * formatInfo.BlockSizeY * formatInfo.BlockSizeZ);
    }
}
