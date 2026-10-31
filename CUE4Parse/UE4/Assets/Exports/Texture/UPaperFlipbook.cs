using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Texture;

public class UPaperFlipbook : UObject
{
    public float FramesPerSecond { get; set; } = 15f;
    public FPaperFlipbookKeyFrame[] KeyFrames { get; set; } = [];
    public FPackageIndex? DefaultMaterial { get; set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        FramesPerSecond = GetOrDefault(nameof(FramesPerSecond), FramesPerSecond);
        KeyFrames = GetOrDefault(nameof(KeyFrames), KeyFrames);
        DefaultMaterial = GetOrDefault<FPackageIndex>(nameof(DefaultMaterial));
    }

    public int GetNumFrames() => KeyFrames.Sum(kf => kf.FrameRun);
    public float GetTotalDuration() => FramesPerSecond != 0 ? GetNumFrames() / FramesPerSecond : 0;
}
