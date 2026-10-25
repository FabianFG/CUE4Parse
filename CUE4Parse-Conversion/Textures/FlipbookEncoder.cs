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
    public static byte[] Encode(this CFlipbook flipbook, ExportOptions options, out string ext, CancellationToken cancellationToken = default) =>
        flipbook.Encode(options.TextureFormat, out ext, options.TextureQuality, options.PngCompressionLevel, cancellationToken: cancellationToken);

    /// <summary>
    /// Encodes the flipbook as an animated WebP, or as an animated PNG for every other format, since JPEG and TGA can't
    /// hold animations. A flipbook without a positive frame rate only shows its first frame, so that becomes a still image.
    /// </summary>
    public static byte[] Encode(this CFlipbook flipbook, ETextureFormat format, out string ext, int quality = 100,
        int pngCompressionLevel = 3, uint loopCount = 0, CancellationToken cancellationToken = default)
    {
        using var image = flipbook.ToImage(loopCount, cancellationToken);
        using var ms = new MemoryStream();

        if (format == ETextureFormat.Webp)
        {
            ext = "webp";
            image.SaveAsWebp(ms, new WebpEncoder
            {
                Quality = quality,
                FileFormat = quality == 100 ? WebpFileFormatType.Lossless : WebpFileFormatType.Lossy
            });
        }
        else
        {
            ext = "png";
            image.SaveAsPng(ms, new PngEncoder { CompressionLevel = (PngCompressionLevel) Math.Clamp(pngCompressionLevel, 0, 9) });
        }
        return ms.ToArray();
    }

    public static Image<Rgba32> ToImage(this CFlipbook flipbook, uint loopCount = 0, CancellationToken cancellationToken = default)
    {
        var animated = flipbook.FramesPerSecond > 0;
        Image<Rgba32>? animation = null;

        foreach (var frame in animated ? flipbook.Frames : flipbook.Frames.Take(1))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var image = ToImage(frame, flipbook.Width, flipbook.Height);
            if (animated)
                SetFrameMetadata(image.Frames.RootFrame.Metadata, frame.FrameRun / flipbook.FramesPerSecond);

            if (animation == null)
            {
                animation = image;
                continue;
            }

            using (image)
                animation.Frames.AddFrame(image.Frames.RootFrame);
        }

        if (animation == null)
            throw new ArgumentException("Flipbook has no frames", nameof(flipbook));

        if (animated)
            SetAnimationMetadata(animation.Metadata, loopCount);
        return animation;
    }

    private static Image<Rgba32> ToImage(CFlipbookFrame frame, int width, int height)
    {
        if (frame.Texture == null)
            return new Image<Rgba32>(width, height);

        var src = frame.Texture.ToImage();
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

    // Every frame covers the whole canvas, so it replaces the previous one instead of being blended over it
    private static void SetFrameMetadata(ImageFrameMetadata frameMeta, double durationSec)
    {
        var pngFrame = frameMeta.GetPngMetadata();
        pngFrame.FrameDelay = ToPngFrameDelay(durationSec);
        pngFrame.DisposalMethod = PngDisposalMethod.RestoreToBackground;
        pngFrame.BlendMethod = PngBlendMethod.Source;

        var webpFrame = frameMeta.GetWebpMetadata();
        webpFrame.FrameDelay = (uint)Math.Clamp(Math.Round(durationSec * 1000.0), 1, 0xFFFFFF);
        webpFrame.DisposalMethod = WebpDisposalMethod.RestoreToBackground;
        webpFrame.BlendMethod = WebpBlendMethod.Source;

        var gifFrame = frameMeta.GetGifMetadata();
        gifFrame.FrameDelay = (int)Math.Clamp(Math.Round(durationSec * 100.0), 1, ushort.MaxValue);
        gifFrame.DisposalMethod = GifDisposalMethod.RestoreToBackground;
    }

    // APNG stores the delay as a fraction of two 16-bit numbers. The first continued fraction convergent within float
    // precision keeps rates like 29.97 at 100/2997, otherwise the closest one that still fits is used.
    private static Rational ToPngFrameDelay(double seconds)
    {
        var (numerator, denominator, previousNumerator, previousDenominator) = (1L, 0L, 0L, 1L);
        var remainder = seconds;
        while (true)
        {
            var whole = (long)Math.Floor(remainder);
            var (nextNumerator, nextDenominator) = (whole * numerator + previousNumerator, whole * denominator + previousDenominator);
            if (nextNumerator > ushort.MaxValue || nextDenominator > ushort.MaxValue)
                break;

            (previousNumerator, previousDenominator, numerator, denominator) = (numerator, denominator, nextNumerator, nextDenominator);
            if (Math.Abs((double)numerator / denominator - seconds) <= seconds * 1e-6 || remainder - whole < 1e-9)
                break;
            remainder = 1 / (remainder - whole);
        }

        return denominator == 0 ? new Rational(ushort.MaxValue, 1) : new Rational((uint)numerator, (uint)denominator);
    }
}
