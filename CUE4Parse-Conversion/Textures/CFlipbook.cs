using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse_Conversion.Textures;

public class CFlipbookFrame(CTexture? texture, int frameRun = 1, FIntPoint offset = default)
{
    public CTexture? Texture { get; } = texture;
    public int FrameRun { get; } = frameRun;
    public FIntPoint Offset { get; } = offset;
}

public class CFlipbook(IReadOnlyList<CFlipbookFrame> frames, float framesPerSecond)
{
    public IReadOnlyList<CFlipbookFrame> Frames { get; } = frames;
    public float FramesPerSecond { get; set; } = framesPerSecond;

    public int Width { get; } = frames.Where(f => f.Texture != null).Select(f => f.Offset.X + f.Texture!.Width).DefaultIfEmpty(0).Max();
    public int Height { get; } = frames.Where(f => f.Texture != null).Select(f => f.Offset.Y + f.Texture!.Height).DefaultIfEmpty(0).Max();

    public int TotalFrames => Frames.Sum(f => f.FrameRun);
    public float Duration => FramesPerSecond != 0 ? TotalFrames / FramesPerSecond : 0;
}
