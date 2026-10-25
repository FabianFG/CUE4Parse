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

        var (x, y) = ((int)sprite.BakedSourceUV.X, (int)sprite.BakedSourceUV.Y);
        var (w, h) = ((int)sprite.BakedSourceDimension.X, (int)sprite.BakedSourceDimension.Y);

        pivot = sprite.BakedRenderData is [var v, ..]
            ? new FIntPoint(
                UnrealMathSSE.RoundToInt(v.Z * sheet.Width - x - v.X * sprite.PixelsPerUnrealUnit),
                UnrealMathSSE.RoundToInt(v.W * sheet.Height - y + v.Y * sprite.PixelsPerUnrealUnit))
            : new FIntPoint((w + 1) / 2, (h + 1) / 2);

        return sheet.Crop(x, y, w, h);
    }
}
