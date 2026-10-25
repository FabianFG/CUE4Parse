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

            if (textureCache != null)
                textureCache[cacheKey] = sheet;
        }

        var (x, y) = ((int)sprite.BakedSourceUV.X, (int)sprite.BakedSourceUV.Y);
        var w = (int)sprite.BakedSourceDimension.X is var dimW && dimW > 0 ? dimW : sheet.Width - x;
        var h = (int)sprite.BakedSourceDimension.Y is var dimH && dimH > 0 ? dimH : sheet.Height - y;

        pivot = sprite.BakedRenderData is [var v, ..]
            ? new FIntPoint(
                (int)MathF.Round(v.Z * sheet.Width - x - v.X * sprite.PixelsPerUnrealUnit),
                (int)MathF.Round(v.W * sheet.Height - y + v.Y * sprite.PixelsPerUnrealUnit))
            : new FIntPoint(w / 2, h / 2);

        return sheet.Crop(x, y, w, h);
    }
}
