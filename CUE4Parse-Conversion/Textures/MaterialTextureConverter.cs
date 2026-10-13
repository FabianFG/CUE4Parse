using System.Numerics;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using SkiaSharp;

namespace CUE4Parse_Conversion.Textures;

/// <summary>Converts material images into PNGs with PBR channels, opacity and normal orientation.</summary>
internal static class MaterialTextureConverter
{
    public static unsafe byte[]? Encode(UTexture texture, ExportOptions options, bool invertNormalY = false,
        UTexture? opacity = null, EMaterialTextureLayout layout = EMaterialTextureLayout.ORM,
        string? sourcePath = null, string? opacityPath = null)
    {
        using var bitmap = ReadTexture(texture, options.TexturePlatform, sourcePath);
        if (bitmap == null)
            return null;

        using var mask = opacity != null ? ReadTexture(opacity, options.TexturePlatform, opacityPath) : null;
        var convert = invertNormalY || mask != null || layout != EMaterialTextureLayout.ORM;
        var width = Math.Max(bitmap.Width, mask?.Width ?? 0);
        var height = Math.Max(bitmap.Height, mask?.Height ?? 0);

        using var prepared = convert && (bitmap.ColorType is not SKColorType.Rgba8888 || bitmap.Width != width || bitmap.Height != height) ? PrepareTexture(bitmap, width, height) : null;
        using var pixels = (prepared ?? bitmap).PeekPixels();
        if (convert)
        {
            var rgba = new Span<byte>(pixels.GetPixels().ToPointer(), pixels.BytesSize);
            NormalizePackedMask(rgba, layout);
            if (invertNormalY)
            {
                ConvertNormal(rgba);
            }
            else if (mask != null)
            {
                ApplyOpacity(rgba, width, height, mask);
            }
        }

        using var encoded = pixels.Encode(TextureEncoder.GetPngOptions(options.PngCompressionLevel));
        return encoded.ToArray();
    }

    private static SKBitmap? ReadTexture(UTexture texture, ETexturePlatform platform, string? path)
    {
        if (path != null)
        {
            using var codec = SKCodec.Create(path) ?? throw new InvalidDataException($"Could not read material texture {path}.");
            var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            return SKBitmap.Decode(codec, info) ?? throw new InvalidDataException($"Could not decode material texture {path}.");
        }

        var index = texture.GetFirstMipIndex();
        var decoded = texture is UTexture2DArray array ? array.DecodeTextureArray(index, platform)?.FirstOrDefault() : texture.DecodeMip(index, platform);
        return decoded?.ToSkBitmap();
    }

    private static unsafe SKBitmap PrepareTexture(SKBitmap bitmap, int width, int height)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        if (width != bitmap.Width || height != bitmap.Height)
            return bitmap.Resize(info, SKFilterQuality.High);
        if (bitmap.ColorType is not (SKColorType.Rgba8888 or SKColorType.Bgra8888))
            return bitmap.Copy(SKColorType.Rgba8888);

        // Skia's color conversion can discard RGB under zero alpha, which normals still need
        var copy = new SKBitmap(info);
        using var source = bitmap.PeekPixels();
        using var target = copy.PeekPixels();
        var original = source.GetPixelSpan();
        var pixels = new Span<byte>(target.GetPixels().ToPointer(), target.BytesSize);
        original.CopyTo(pixels);
        if (bitmap.ColorType is SKColorType.Bgra8888)
        {
            for (var i = 0; i < pixels.Length; i += 4)
            {
                (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
            }
        }

        return copy;
    }

    private static void NormalizePackedMask(Span<byte> pixels, EMaterialTextureLayout layout)
    {
        if (layout is EMaterialTextureLayout.ORM)
            return;

        var (occlusion, roughness, metallic) = layout switch
        {
            EMaterialTextureLayout.RMA => (2, 0, 1),
            EMaterialTextureLayout.ROM => (1, 0, 2),
            EMaterialTextureLayout.RSM => (-1, 0, 2),
            EMaterialTextureLayout.MRA or EMaterialTextureLayout.MRO => (2, 1, 0),
            EMaterialTextureLayout.SRM => (-1, 1, 2),
            _ => throw new ArgumentOutOfRangeException(nameof(layout), layout, null)
        };

        for (var i = 0; i < pixels.Length; i += 4)
        {
            var roughnessValue = pixels[i + roughness];
            var metallicValue = pixels[i + metallic];
            var occlusionValue = byte.MaxValue;
            if (occlusion >= 0)
            {
                occlusionValue = pixels[i + occlusion];
            }

            (pixels[i], pixels[i + 1], pixels[i + 2]) = (occlusionValue, roughnessValue, metallicValue);
        }
    }

    // Convert DirectX tangent space normals to the OpenGL convention
    private static void ConvertNormal(Span<byte> pixels)
    {
        // Each uint holds one RGBA pixel: XOR flips green, OR makes alpha opaque
        var greenMask = new Vector<uint>(BitConverter.IsLittleEndian ? 0x0000FF00u : 0x00FF0000u);
        var alphaMask = new Vector<uint>(BitConverter.IsLittleEndian ? 0xFF000000u : 0x000000FFu);
        var blocks = MemoryMarshal.Cast<byte, Vector<uint>>(pixels);
        foreach (ref var block in blocks)
        {
            block = (block ^ greenMask) | alphaMask;
        }

        for (var i = blocks.Length * Vector<byte>.Count; i < pixels.Length; i += 4)
        {
            pixels[i + 1] = (byte) (255 - pixels[i + 1]);
            pixels[i + 3] = 255;
        }
    }

    private static void ApplyOpacity(Span<byte> pixels, int width, int height, SKBitmap mask)
    {
        using var rgba = mask.ColorType is not SKColorType.Rgba8888 ? mask.Copy(SKColorType.Rgba8888) : null;
        using var maskPixels = (rgba ?? mask).PeekPixels();
        var values = maskPixels.GetPixelSpan();
        if (width == mask.Width && height == mask.Height)
        {
            for (var i = 0; i < pixels.Length; i += 4)
            {
                pixels[i + 3] = values[i];
            }

            return;
        }

        for (var y = 0; y < height; y++)
        {
            var maskRow = y * mask.Height / height * mask.Width;
            var pixelRow = y * width;
            for (var x = 0; x < width; x++)
            {
                var maskIndex = (maskRow + x * mask.Width / width) * 4;
                pixels[(pixelRow + x) * 4 + 3] = values[maskIndex];
            }
        }
    }
}
