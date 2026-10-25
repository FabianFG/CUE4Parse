using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

[StructFallback]
public class FPaperFlipbookKeyFrame : IUStruct
{
    public FPackageIndex? Sprite { get; set; }
    public int FrameRun { get; set; } = 1;

    public FPaperFlipbookKeyFrame() { }

    public FPaperFlipbookKeyFrame(FPackageIndex? sprite, int frameRun = 1)
    {
        Sprite = sprite;
        FrameRun = frameRun;
    }

    public FPaperFlipbookKeyFrame(FStructFallback fallback)
    {
        Sprite = fallback.GetOrDefault<FPackageIndex>(nameof(Sprite));
        FrameRun = fallback.GetOrDefault(nameof(FrameRun), 1);
    }

    public UPaperSprite? LoadSprite() => Sprite?.Load<UPaperSprite>();
    public bool TryLoadSprite(out UPaperSprite? sprite) => (sprite = LoadSprite()) is not null;
}
