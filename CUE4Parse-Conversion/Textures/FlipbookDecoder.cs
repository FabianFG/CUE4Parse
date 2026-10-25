using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Textures;

public static class FlipbookDecoder
{
    public static CFlipbook? Decode(this UPaperFlipbook flipbook, ETexturePlatform platform = ETexturePlatform.DesktopMobile, IDictionary<string, CTexture>? textureCache = null)
    {
        if (flipbook.KeyFrames.Length == 0) return null;

        textureCache ??= new Dictionary<string, CTexture>();

        var rawFrames = new (CTexture? Texture, int FrameRun, FIntPoint Pivot)[flipbook.KeyFrames.Length];
        var (maxPivotX, maxPivotY) = (int.MinValue, int.MinValue);

        for (int i = 0; i < flipbook.KeyFrames.Length; i++)
        {
            var kf = flipbook.KeyFrames[i];
            CTexture? spriteTex = null;
            FIntPoint pivot = default;

            if (kf.Sprite is { IsNull: false })
            {
                spriteTex = kf.LoadSprite()?.Decode(out pivot, platform, textureCache);
                if (spriteTex == null)
                {
                    Log.Warning("Flipbook '{Name}': Failed to decode keyframe {Index}, aborting flipbook decode", flipbook.Name, i);
                    return null;
                }

                if (pivot.X > maxPivotX) maxPivotX = pivot.X;
                if (pivot.Y > maxPivotY) maxPivotY = pivot.Y;
            }

            rawFrames[i] = (spriteTex, kf.FrameRun, pivot);
        }

        if (rawFrames.All(f => f.Texture == null)) return null;

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
