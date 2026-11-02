using CUE4Parse.UE4.Assets.Exports.Paper2D;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse_Conversion.Options;

namespace CUE4Parse_Conversion.Textures.Paper2D;

public static class SpriteDecoder
{
    public static CTexture? Decode(this UPaperSprite sprite, ExportOptions options, IDictionary<string, CTexture>? textureCache = null, CancellationToken cancellationToken = default)
        => sprite.Decode(out _, options, textureCache, cancellationToken);

    public static CTexture? Decode(this UPaperSprite sprite, out FIntPoint pivot, ExportOptions options, IDictionary<string, CTexture>? textureCache = null, CancellationToken cancellationToken = default)
        => sprite.Decode(out pivot, out _, options, textureCache, cancellationToken);

    // scale is the size of the decoded mip relative to the imported texture the sprite's coordinates refer to
    internal static CTexture? Decode(this UPaperSprite sprite, out FIntPoint pivot, out float scale, ExportOptions options, IDictionary<string, CTexture>? textureCache, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        pivot = default;
        scale = 1;

        var sourceTexture = sprite.BakedSourceTexture?.Load<UTexture2D>();
        if (sourceTexture == null)
            return null;

        var cacheKey = sourceTexture.GetPathName();
        if (textureCache == null || !textureCache.TryGetValue(cacheKey, out var sheet))
        {
            sheet = sourceTexture.Decode(options, cancellationToken);
            if (sheet == null)
                return null;

            textureCache?[cacheKey] = sheet;
        }

        if (sourceTexture.ImportedSize.X > 0)
            scale = (float) sheet.Width / sourceTexture.ImportedSize.X;

        var decoded = sprite.Decode(sheet, sourceTexture.ImportedSize, out pivot);
        cancellationToken.ThrowIfCancellationRequested();
        return decoded;
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
        {
            (u, v, w, h) = (0, 0, importedWidth, importedHeight);
        }

        // Sprite sheets can be split into fractional cells. Rounding the origin and the size separately picks the texels
        // UE samples and keeps every frame equally large.
        var x0 = Math.Clamp(UnrealMathSSE.RoundToInt(u * scaleX), 0, sheet.Width - 1);
        var y0 = Math.Clamp(UnrealMathSSE.RoundToInt(v * scaleY), 0, sheet.Height - 1);
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
        var upright = Spread(v => v.X * ppuu - v.Z * width) + Spread(v => v.Y * ppuu + v.W * height);
        var rotated = Spread(v => v.X * ppuu - v.W * height) + Spread(v => v.Y * ppuu - v.Z * width);
        return rotated < upright;

        float Spread(Func<FVector4, float> offset) => renderData.Max(offset) - renderData.Min(offset);
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
