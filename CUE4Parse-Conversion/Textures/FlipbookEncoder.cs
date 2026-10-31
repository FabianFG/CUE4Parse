using CUE4Parse_Conversion.Options;
using SixLabors.ImageSharp;
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
            // The async overloads take the token into the encoders, the stream is in memory so nothing actually waits
            image.SaveAsWebpAsync(ms, new WebpEncoder
            {
                Quality = quality,
                FileFormat = quality == 100 ? WebpFileFormatType.Lossless : WebpFileFormatType.Lossy
            }, cancellationToken).GetAwaiter().GetResult();
        }
        else
        {
            ext = "png";
            image.SaveAsPngAsync(ms, new PngEncoder { CompressionLevel = (PngCompressionLevel) Math.Clamp(pngCompressionLevel, 0, 9) }, cancellationToken)
                .GetAwaiter().GetResult();
        }
        return ms.ToArray();
    }

    public static Image<Rgba32> ToImage(this CFlipbook flipbook, uint loopCount = 0, CancellationToken cancellationToken = default)
    {
        if (flipbook.Frames.Count == 0)
            throw new ArgumentException("Flipbook has no frames", nameof(flipbook));

        // Without a positive frame rate UE only shows the first frame, as it is, without the canvas the animation needs
        if (flipbook.FramesPerSecond <= 0)
        {
            var first = flipbook.Frames[0];
            return first.Texture?.ToImage() ?? new Image<Rgba32>(Math.Max(flipbook.Width, 1), Math.Max(flipbook.Height, 1));
        }

        Image<Rgba32>? animation = null;
        var (elapsedFrames, elapsedMilliseconds) = (0, 0L);
        foreach (var frame in flipbook.Frames)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // WebP delays are whole milliseconds. Rounding where each frame ends instead of each frame's length keeps
            // the rounding error from adding up over the loop
            elapsedFrames += frame.FrameRun;
            var endMilliseconds = (long) Math.Round(elapsedFrames * 1000.0 / flipbook.FramesPerSecond);
            var image = ToImage(frame, flipbook.Width, flipbook.Height);
            SetFrameMetadata(image.Frames.RootFrame.Metadata, frame.FrameRun / flipbook.FramesPerSecond, endMilliseconds - elapsedMilliseconds);
            elapsedMilliseconds = endMilliseconds;

            if (animation == null)
            {
                animation = image;
                continue;
            }

            using (image)
                animation.Frames.AddFrame(image.Frames.RootFrame);
        }

        SetAnimationMetadata(animation!.Metadata, loopCount);
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

        metadata.GetWebpMetadata().RepeatCount = (ushort)Math.Min(loopCount, ushort.MaxValue);
    }

    // Every frame covers the whole canvas, so it replaces the previous one instead of being blended over it
    private static void SetFrameMetadata(ImageFrameMetadata frameMeta, double durationSec, long webpDelayMilliseconds)
    {
        var pngFrame = frameMeta.GetPngMetadata();
        pngFrame.FrameDelay = ToPngFrameDelay(durationSec);
        pngFrame.DisposalMethod = PngDisposalMethod.RestoreToBackground;
        pngFrame.BlendMethod = PngBlendMethod.Source;

        var webpFrame = frameMeta.GetWebpMetadata();
        webpFrame.FrameDelay = (uint)Math.Clamp(webpDelayMilliseconds, 1, 0xFFFFFF);
        webpFrame.DisposalMethod = WebpDisposalMethod.RestoreToBackground;
        webpFrame.BlendMethod = WebpBlendMethod.Source;
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
