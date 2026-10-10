using System.Numerics;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Options;
using SkiaSharp;

namespace CUE4Parse_Conversion.Textures;

/// <summary>Decodes material textures and converts packed channels, normal orientation and opacity.</summary>
public sealed class MaterialTextureConverter(ExportOptions options, TextureExportCache? textures = null)
{
    public CTexture? GetTexture(UTexture texture, bool isNormalMap = false, UTexture? opacity = null, EMaterialTextureLayout layout = EMaterialTextureLayout.ORM)
    {
        var decoded = Decode(texture);
        if (decoded == null)
            return null;

        var invertNormalY = isNormalMap && options.MeshFormat is EMeshFormat.Gltf2;
        if (!invertNormalY && opacity == null && layout is EMaterialTextureLayout.ORM)
            return decoded;

        var mask = opacity != null ? Decode(opacity) : null;
        // A tiny solid color base texture must not reduce a detailed eyelash/hair mask
        var width = Math.Max(decoded.Width, mask?.Width ?? 0);
        var height = Math.Max(decoded.Height, mask?.Height ?? 0);
        var pixels = GetRgbaPixels(decoded, width, height);

        NormalizePackedMask(pixels, layout);

        if (invertNormalY)
        {
            ConvertNormal(pixels);
        }
        else if (mask != null)
        {
            ApplyOpacity(pixels, width, height, mask);
        }

        return new CTexture(width, height, EPixelFormat.PF_R8G8B8A8, pixels);
    }

    private CTexture? Decode(UTexture texture) => textures != null ? textures.Decode(texture) : texture.Decode(options.TexturePlatform);

    private static byte[] GetRgbaPixels(CTexture texture, int width, int height)
    {
        if (width == texture.Width && height == texture.Height)
        {
            if (texture.PixelFormat is EPixelFormat.PF_R8G8B8A8)
                return (byte[]) texture.Data.Clone();

            if (texture.PixelFormat is EPixelFormat.PF_B8G8R8A8 or EPixelFormat.PF_A8R8G8B8)
            {
                var pixels = (byte[]) texture.Data.Clone();
                for (var i = 0; i < pixels.Length; i += 4)
                {
                    (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
                }

                return pixels;
            }
        }

        using var bitmap = texture.ToSkBitmap();
        using var rgba = bitmap.Resize(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul), SKFilterQuality.High);
        return rgba.Bytes;
    }

    private static void NormalizePackedMask(byte[] pixels, EMaterialTextureLayout layout)
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
    private static void ConvertNormal(byte[] pixels)
    {
        // Each uint holds one RGBA pixel: XOR flips green, OR makes alpha opaque
        var greenMask = new Vector<uint>(BitConverter.IsLittleEndian ? 0x0000FF00u : 0x00FF0000u);
        var alphaMask = new Vector<uint>(BitConverter.IsLittleEndian ? 0xFF000000u : 0x000000FFu);
        var blocks = MemoryMarshal.Cast<byte, Vector<uint>>(pixels.AsSpan());
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

    // Combine the separate opacity mask with base color alpha
    private static void ApplyOpacity(byte[] pixels, int width, int height, CTexture mask)
    {
        var maskPixels = GetRgbaPixels(mask, mask.Width, mask.Height);
        if (width == mask.Width && height == mask.Height)
        {
            for (var i = 0; i < pixels.Length; i += 4)
            {
                pixels[i + 3] = maskPixels[i];
            }
            return;
        }

        for (var y = 0; y < height; y++)
        {
            var maskRow = y * mask.Height / height * mask.Width;
            var pixelRow = y * width;
            for (var x = 0; x < width; x++)
            {
                var maskIndex = maskRow + x * mask.Width / width;
                pixels[(pixelRow + x) * 4 + 3] = maskPixels[maskIndex * 4];
            }
        }
    }
}
