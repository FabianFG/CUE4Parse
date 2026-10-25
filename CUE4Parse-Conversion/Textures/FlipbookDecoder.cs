using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Textures;

public static class FlipbookDecoder
{
    public static CFlipbook? Decode(this UPaperFlipbook flipbook, ETexturePlatform platform = ETexturePlatform.DesktopMobile, IDictionary<string, CTexture>? textureCache = null, CancellationToken cancellationToken = default)
    {
        if (flipbook.KeyFrames.Length == 0) return null;

        textureCache ??= new Dictionary<string, CTexture>();
        // Key frames often reuse a sprite, so every sprite is only cut out once
        var sprites = new Dictionary<UPaperSprite, (CTexture Texture, FIntPoint Pivot)>();

        var rawFrames = new (CTexture? Texture, int FrameRun, FIntPoint Pivot)[flipbook.KeyFrames.Length];
        var (maxPivotX, maxPivotY) = (int.MinValue, int.MinValue);

        for (int i = 0; i < flipbook.KeyFrames.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var kf = flipbook.KeyFrames[i];
            CTexture? spriteTex = null;
            FIntPoint pivot = default;

            if (kf.Sprite is { IsNull: false })
            {
                var sprite = kf.LoadSprite();
                if (sprite == null || !sprites.TryGetValue(sprite, out var decoded))
                {
                    if (sprite?.Decode(out pivot, platform, textureCache) is not { } texture)
                    {
                        Log.Warning("Flipbook '{Name}': Failed to decode keyframe {Index}, aborting flipbook decode", flipbook.Name, i);
                        return null;
                    }

                    sprites[sprite] = decoded = (texture, pivot);
                }

                (spriteTex, pivot) = decoded;

                if (pivot.X > maxPivotX) maxPivotX = pivot.X;
                if (pivot.Y > maxPivotY) maxPivotY = pivot.Y;
            }

            rawFrames[i] = (spriteTex, kf.FrameRun, pivot);
        }

        if (rawFrames.All(f => f.Texture == null)) return null;

        if (sprites.Keys.Select(sprite => sprite.PixelsPerUnrealUnit).Distinct().Count() > 1)
            Log.Warning("Flipbook '{Name}' mixes sprites with different pixels per unit, their frames are not rescaled", flipbook.Name);

        var frames = new CFlipbookFrame[rawFrames.Length];
        for (int i = 0; i < rawFrames.Length; i++)
        {
            var (tex, frameRun, pivot) = rawFrames[i];
            var offset = tex != null ? new FIntPoint(maxPivotX - pivot.X, maxPivotY - pivot.Y) : default;
            frames[i] = new CFlipbookFrame(tex, frameRun, offset);
        }

        return new CFlipbook(frames, flipbook.FramesPerSecond);
    }
}
