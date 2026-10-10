using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Options;

namespace CUE4Parse_Conversion.Textures;

/// <summary>Shares texture decoding and encoding between outputs in one export run.</summary>
public sealed class TextureExportCache(ETexturePlatform platform)
{
    private readonly ConcurrentDictionary<(object Provider, string Path), Lazy<CTexture?>> _decoded = new();
    private readonly ConditionalWeakTable<CTexture, ConcurrentDictionary<(ETextureFormat Format, bool Hdr, int Quality, int PngCompression, bool TgaRle), Lazy<ExportFile>>> _encoded = [];

    public CTexture? Decode(UTexture texture, int? mipIndex = null)
    {
        var firstMip = texture.GetFirstMipIndex();
        var index = mipIndex ?? firstMip;
        if (texture.PlatformData is { FirstMipToSerialize: >= 0, VTData: { } vt } && vt.IsInitialized())
        {
            firstMip = TextureDecoder.GetMinLevel(vt);
            index = Math.Max(index, firstMip);
        }

        // Only the mip used by embedded materials is needed
        if (index != firstMip)
            return texture.DecodeMip(index, platform);

        // Separate package loads can create different objects for the same texture
        var provider = (object?) texture.Owner?.Provider ?? texture;
        var key = (provider, texture.GetPathName().ToUpperInvariant());
        return _decoded.GetOrAdd(key, _ => new Lazy<CTexture?>(() => texture.DecodeMip(index, platform))).Value;
    }

    public ExportFile Encode(CTexture texture, ExportOptions options)
        => Encode(texture, options.TextureFormat, options.ExportHdrTexturesAsHdr, options.TextureQuality, options.PngCompressionLevel, options.TgaRleCompression);

    public ExportFile Encode(CTexture texture, ETextureFormat format, bool saveHdrAsHdr, int quality = 100, int pngCompressionLevel = 3, bool tgaRleCompression = true)
    {
        var bIsHdr = saveHdrAsHdr && PixelFormatUtils.IsHDR(texture.PixelFormat);

        // Exclude settings that do not affect this encoding from the cache key
        if (bIsHdr || format is ETextureFormat.Png or ETextureFormat.Tga)
        {
            quality = 100;
        }

        pngCompressionLevel = !bIsHdr && format == ETextureFormat.Png ? Math.Clamp(pngCompressionLevel, 0, 9) : 0;
        tgaRleCompression &= !bIsHdr && format == ETextureFormat.Tga;

        var encodings = _encoded.GetOrCreateValue(texture);
        return encodings.GetOrAdd((format, bIsHdr, quality, pngCompressionLevel, tgaRleCompression), key => new Lazy<ExportFile>(() =>
        {
            var data = texture.Encode(key.Format, key.Hdr, out var extension, key.Quality, key.PngCompression, key.TgaRle);
            return new ExportFile(extension, data);
        })).Value;
    }
}
