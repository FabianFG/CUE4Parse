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
        var sprites = new Dictionary<UPaperSprite, (CTexture Texture, FIntPoint Pivot, float Scale)?>();

        var keyFrames = new (CTexture? Texture, int FrameRun, FIntPoint Pivot)[flipbook.KeyFrames.Length];
        for (int i = 0; i < flipbook.KeyFrames.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var kf = flipbook.KeyFrames[i];
            keyFrames[i] = (null, kf.FrameRun, default);
            if (kf.Sprite is not { IsNull: false } || kf.FrameRun <= 0)
                continue;

            // A sprite that can't be loaded or decoded leaves its frame empty, like a key frame without a sprite
            var sprite = kf.LoadSprite();
            if (sprite == null)
            {
                Log.Warning("Flipbook '{Name}': Could not load the sprite of key frame {Index}, the frame stays empty", flipbook.Name, i);
                continue;
            }

            if (!sprites.TryGetValue(sprite, out var decoded))
            {
                decoded = sprite.Decode(out var pivot, out var scale, platform, textureCache) is { } texture ? (texture, pivot, scale) : null;
                if (decoded == null)
                    Log.Warning("Flipbook '{Name}': Could not decode sprite '{Sprite}' of key frame {Index}, the frame stays empty", flipbook.Name, sprite.Name, i);
                sprites[sprite] = decoded;
            }

            if (decoded is { } value)
                keyFrames[i] = (value.Texture, kf.FrameRun, value.Pivot);
        }

        var decodedSprites = sprites.Where(pair => pair.Value != null).ToList();
        if (decodedSprites.Select(pair => pair.Key.PixelsPerUnrealUnit).Distinct().Count() > 1)
            Log.Warning("Flipbook '{Name}' mixes sprites with different pixels per unit, their frames are not rescaled", flipbook.Name);
        if (decodedSprites.Select(pair => MathF.Round(pair.Value!.Value.Scale, 3)).Distinct().Count() > 1)
            Log.Warning("Flipbook '{Name}' mixes sprites cut from different texture mip sizes, their frames are not rescaled", flipbook.Name);

        return Assemble(keyFrames, flipbook.FramesPerSecond);
    }

    /// <summary>
    /// Lines the key frames up on their pivots. Key frames with a run of 0 or less never show in UE, so they are left out.
    /// </summary>
    internal static CFlipbook? Assemble(IReadOnlyList<(CTexture? Texture, int FrameRun, FIntPoint Pivot)> keyFrames, float framesPerSecond)
    {
        var shown = keyFrames.Where(frame => frame.FrameRun > 0).ToList();
        if (shown.All(frame => frame.Texture == null)) return null;

        var maxPivotX = shown.Where(frame => frame.Texture != null).Max(frame => frame.Pivot.X);
        var maxPivotY = shown.Where(frame => frame.Texture != null).Max(frame => frame.Pivot.Y);

        var frames = new CFlipbookFrame[shown.Count];
        for (int i = 0; i < shown.Count; i++)
        {
            var (tex, frameRun, pivot) = shown[i];
            var offset = tex != null ? new FIntPoint(maxPivotX - pivot.X, maxPivotY - pivot.Y) : default;
            frames[i] = new CFlipbookFrame(tex, frameRun, offset);
        }

        return new CFlipbook(frames, framesPerSecond);
    }
}
