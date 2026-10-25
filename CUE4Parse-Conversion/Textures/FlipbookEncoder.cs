using CUE4Parse_Conversion.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CUE4Parse_Conversion.Textures;

public static class FlipbookEncoder
{
    public static byte[] Encode(this CFlipbook flipbook, ExportOptions options, out string ext) =>
        flipbook.Encode(options.TextureFormat, out ext, options.TextureQuality);

    public static byte[] Encode(this CFlipbook flipbook, ETextureFormat format, out string ext, int quality = 100, uint loopCount = 0)
    {
        using var image = flipbook.ToImage(loopCount);
        using var ms = new MemoryStream();

        switch (format)
        {
            case ETextureFormat.Png:
                ext = "png";
                image.SaveAsPng(ms);
                break;
            case ETextureFormat.Webp:
                ext = "webp";
                image.SaveAsWebp(ms, new WebpEncoder
                {
                    Quality = quality,
                    FileFormat = quality == 100 ? WebpFileFormatType.Lossless : WebpFileFormatType.Lossy
                });
                break;
            default:
                throw new NotImplementedException("Unsupported texture format: " + format);
        }
        return ms.ToArray();
    }

    public static Image<Rgba32> ToImage(this CFlipbook flipbook, uint loopCount = 0)
    {
        var fps = Rational.FromDouble(flipbook.FramesPerSecond);
        var animation = CreateFrame(flipbook.Frames[0], flipbook.Width, flipbook.Height, fps);

        foreach (var frame in flipbook.Frames.Skip(1))
        {
            using var nextFrame = CreateFrame(frame, flipbook.Width, flipbook.Height, fps);
            animation.Frames.AddFrame(nextFrame.Frames.RootFrame);
        }

        SetAnimationMetadata(animation.Metadata, loopCount);
        return animation;
    }

    private static Image<Rgba32> CreateFrame(CFlipbookFrame frame, int width, int height, Rational fps)
    {
        var image = ToImage(frame, width, height);
        SetFrameMetadata(image.Frames.RootFrame.Metadata, frame.FrameRun, fps);
        return image;
    }

    private static Image<Rgba32> ToImage(CFlipbookFrame frame, int width, int height)
    {
        if (frame.Texture is not { } tex)
            return new Image<Rgba32>(width, height);

        var src = tex.ToImage();
        if (src.Width == width && src.Height == height)
            return src;

        var canvas = new Image<Rgba32>(width, height);
        canvas.Mutate(ctx => ctx.DrawImage(src, new Point(frame.Offset.X, frame.Offset.Y), opacity: 1f));
        src.Dispose();
        return canvas;
    }

    private static void SetAnimationMetadata(ImageMetadata metadata, uint loopCount)
    {
        var pngMeta = metadata.GetPngMetadata();
        pngMeta.RepeatCount = loopCount;
        pngMeta.AnimateRootFrame = true;

        var repeatCount16 = (ushort)Math.Min(loopCount, ushort.MaxValue);
        metadata.GetWebpMetadata().RepeatCount = repeatCount16;
        metadata.GetGifMetadata().RepeatCount = repeatCount16;
    }

    private static void SetFrameMetadata(ImageFrameMetadata frameMeta, int frameRun, Rational fps)
    {
        var frameDelay = new Rational((uint)frameRun * fps.Denominator, fps.Numerator);
        var durationSec = frameDelay.ToDouble();

        var pngFrame = frameMeta.GetPngMetadata();
        pngFrame.FrameDelay = frameDelay;
        pngFrame.DisposalMethod = PngDisposalMethod.RestoreToBackground;

        frameMeta.GetWebpMetadata().FrameDelay = (uint)Math.Max(1, Math.Round(durationSec * 1000.0));

        var gifFrame = frameMeta.GetGifMetadata();
        gifFrame.FrameDelay = (int)Math.Max(1, Math.Round(durationSec * 100.0));
        gifFrame.DisposalMethod = GifDisposalMethod.RestoreToBackground;
    }
}
