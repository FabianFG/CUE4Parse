using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Textures;

public static class SpriteDecoder
{
    public static CTexture? Decode(this UPaperSprite sprite, ETexturePlatform platform = ETexturePlatform.DesktopMobile, IDictionary<string, CTexture>? textureCache = null) =>
        sprite.Decode(out _, platform, textureCache);

    public static CTexture? Decode(this UPaperSprite sprite, out FIntPoint pivot, ETexturePlatform platform = ETexturePlatform.DesktopMobile, IDictionary<string, CTexture>? textureCache = null)
    {
        pivot = default;

        var sourceTexture = sprite.BakedSourceTexture?.Load<UTexture2D>();
        if (sourceTexture == null) return null;

        var cacheKey = sourceTexture.GetPathName();
        if (textureCache == null || !textureCache.TryGetValue(cacheKey, out var sheet))
        {
            sheet = sourceTexture.Decode(platform);
            if (sheet == null) return null;
            textureCache?[cacheKey] = sheet;
        }

        return sprite.Decode(sheet, sourceTexture.ImportedSize, out pivot);
    }

    /// <summary>
    /// Cuts the sprite out of its decoded source texture. The baked rectangle and render data are in pixels of the
    /// imported texture, which can be larger than the decoded mip.
    /// </summary>
    public static CTexture Decode(this UPaperSprite sprite, CTexture sheet, FIntPoint importedSize, out FIntPoint pivot)
    {
        var (importedWidth, importedHeight) = importedSize is { X: > 0, Y: > 0 } ? (importedSize.X, importedSize.Y) : (sheet.Width, sheet.Height);
        var (scaleX, scaleY) = ((float) sheet.Width / importedWidth, (float) sheet.Height / importedHeight);

        var (u, v) = (sprite.BakedSourceUV.X, sprite.BakedSourceUV.Y);
        var (w, h) = (sprite.BakedSourceDimension.X, sprite.BakedSourceDimension.Y);
        if (w <= 0 || h <= 0)
            (u, v, w, h) = (0, 0, importedWidth, importedHeight);

        // Sprite sheets can be split into fractional cells, rounding only the size keeps every frame equally large
        var x0 = Math.Clamp((int) MathF.Floor(u * scaleX), 0, sheet.Width - 1);
        var y0 = Math.Clamp((int) MathF.Floor(v * scaleY), 0, sheet.Height - 1);
        var cropWidth = Math.Clamp(UnrealMathSSE.RoundToInt(w * scaleX), 1, sheet.Width - x0);
        var cropHeight = Math.Clamp(UnrealMathSSE.RoundToInt(h * scaleY), 1, sheet.Height - y0);
        var crop = sheet.Crop(x0, y0, cropWidth, cropHeight);

        if (sprite.BakedRenderData is not [var vertex, ..])
        {
            pivot = new FIntPoint((crop.Width + 1) / 2, (crop.Height + 1) / 2);
            return crop;
        }

        // Undo UPaperSprite::ConvertTextureSpaceToPivotSpace for one vertex to find the pivot in texture pixels
        var ppuu = sprite.PixelsPerUnrealUnit;
        var rotated = IsRotatedInSourceImage(sprite.BakedRenderData, ppuu, importedWidth, importedHeight);
        var (pivotX, pivotY) = rotated
            ? (vertex.Z * importedWidth - vertex.Y * ppuu, vertex.W * importedHeight - vertex.X * ppuu)
            : (vertex.Z * importedWidth - vertex.X * ppuu, vertex.W * importedHeight + vertex.Y * ppuu);
        var (localX, localY) = (pivotX * scaleX - x0, pivotY * scaleY - y0);

        if (!rotated)
        {
            pivot = new FIntPoint(UnrealMathSSE.RoundToInt(localX), UnrealMathSSE.RoundToInt(localY));
            return crop;
        }

        pivot = new FIntPoint(UnrealMathSSE.RoundToInt(localY), UnrealMathSSE.RoundToInt(crop.Width - localX));
        return RotateCounterClockwise(crop);
    }

    // bRotatedInSourceImage is editor-only, but rotated sprites swap which texture axis follows which position axis
    private static bool IsRotatedInSourceImage(FVector4[] renderData, float ppuu, int width, int height)
    {
        float Spread(Func<FVector4, float> offset) => renderData.Max(offset) - renderData.Min(offset);

        var upright = Spread(v => v.X * ppuu - v.Z * width) + Spread(v => v.Y * ppuu + v.W * height);
        var rotated = Spread(v => v.X * ppuu - v.W * height) + Spread(v => v.Y * ppuu - v.Z * width);
        return rotated < upright;
    }

    // Atlases store rotated sprites turned 90 degrees clockwise
    private static CTexture RotateCounterClockwise(CTexture texture)
    {
        var bpp = PixelFormatUtils.PixelFormats[texture.PixelFormat].BlockBytes;
        var (width, height) = (texture.Height, texture.Width);
        var data = new byte[texture.Data.Length];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var source = (x * texture.Width + texture.Width - 1 - y) * bpp;
                Buffer.BlockCopy(texture.Data, source, data, (y * width + x) * bpp, bpp);
            }
        }

        return new CTexture(width, height, texture.PixelFormat, data);
    }
}
