using CUE4Parse_Conversion.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CUE4Parse_Conversion.Textures.Paper2D;

public static class FlipbookEncoder
{
    public static byte[] Encode(this CFlipbook flipbook, ExportOptions options, out string ext, CancellationToken cancellationToken = default)
    {
        using var image = flipbook.ToImage(options, cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        using var ms = new MemoryStream();
        switch (options.AnimatedTextureFormat)
        {
            case EAnimatedTextureFormat.Webp:
                ext = "webp";
                image.SaveAsWebp(ms, new WebpEncoder
                {
                    Quality = options.TextureQuality,
                    FileFormat = options.TextureQuality == 100 ? WebpFileFormatType.Lossless : WebpFileFormatType.Lossy
                });
                break;
            case EAnimatedTextureFormat.Gif:
                ext = "gif";
                image.SaveAsGif(ms);
                break;
            case EAnimatedTextureFormat.Png:
                ext = "png";
                image.SaveAsPng(ms, new PngEncoder { CompressionLevel = (PngCompressionLevel) options.PngCompressionLevel });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options), options.AnimatedTextureFormat, "Unsupported animated texture format");
        }

        return ms.ToArray();
    }

    public static Image<Rgba32> ToImage(this CFlipbook flipbook, ExportOptions options, int maxSize = 0, CancellationToken cancellationToken = default)
    {
        if (flipbook.Frames.Count == 0)
            throw new ArgumentException("Flipbook has no frames", nameof(flipbook));

        // Without a positive frame rate UE only shows the first frame, as it is, without the canvas the animation needs
        if (flipbook.FramesPerSecond <= 0)
        {
            var first = flipbook.Frames[0];
            return first.Texture?.ToImage() ?? new Image<Rgba32>(Math.Max(flipbook.Width, 1), Math.Max(flipbook.Height, 1));
        }

        var scale = maxSize > 0 ? Math.Min(1.0, (double) maxSize / Math.Max(flipbook.Width, flipbook.Height)) : 1.0;
        var width = Math.Max(1, (int) Math.Round(flipbook.Width * scale));
        var height = Math.Max(1, (int) Math.Round(flipbook.Height * scale));

        Image<Rgba32>? animation = null;
        try
        {
            var (elapsedFrames, elapsedMilliseconds, elapsedCentiseconds) = (0, 0L, 0L);
            foreach (var frame in flipbook.Frames)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Round cumulative frame times so WebP and GIF delays don't drift over the loop.
                elapsedFrames += frame.FrameRun;
                var endMilliseconds = (long) Math.Round(elapsedFrames * 1000.0 / flipbook.FramesPerSecond);
                var endCentiseconds = (long) Math.Round(elapsedFrames * 100.0 / flipbook.FramesPerSecond);
                var image = ToImage(frame, width, height, scale);
                if (animation == null)
                {
                    animation = image;
                }
                else
                {
                    using (image)
                    {
                        animation.Frames.AddFrame(image.Frames.RootFrame);
                    }
                }

                SetFrameMetadata(animation.Frames[^1].Metadata, options, frame.FrameRun / flipbook.FramesPerSecond, endMilliseconds - elapsedMilliseconds, endCentiseconds - elapsedCentiseconds);
                elapsedMilliseconds = endMilliseconds;
                elapsedCentiseconds = endCentiseconds;
            }

            SetAnimationMetadata(animation!.Metadata, options);
            return animation;
        }
        catch
        {
            animation?.Dispose();
            throw;
        }
    }

    private static Image<Rgba32> ToImage(CFlipbookFrame frame, int width, int height, double scale)
    {
        if (frame.Texture == null)
            return new Image<Rgba32>(width, height);

        var src = frame.Texture.ToImage();
        if (scale < 1.0)
        {
            src.Mutate(ctx => ctx.Resize(Math.Max(1, (int) Math.Round(src.Width * scale)), Math.Max(1, (int) Math.Round(src.Height * scale)), KnownResamplers.NearestNeighbor));
        }

        if (src.Width == width && src.Height == height)
            return src;

        var canvas = new Image<Rgba32>(width, height);
        canvas.Mutate(ctx => ctx.DrawImage(src, new Point((int) Math.Round(frame.Offset.X * scale), (int) Math.Round(frame.Offset.Y * scale)), opacity: 1f));
        src.Dispose();
        return canvas;
    }

    private static void SetAnimationMetadata(ImageMetadata metadata, ExportOptions options)
    {
        switch (options.AnimatedTextureFormat)
        {
            case EAnimatedTextureFormat.Png:
                var pngMeta = metadata.GetPngMetadata();
                pngMeta.RepeatCount = 0;
                pngMeta.AnimateRootFrame = true;
                break;
            case EAnimatedTextureFormat.Webp:
                metadata.GetWebpMetadata().RepeatCount = 0;
                break;
            case EAnimatedTextureFormat.Gif:
                metadata.GetGifMetadata().RepeatCount = 0;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options), options.AnimatedTextureFormat, "Unsupported animated texture format");
        }
    }

    // Every frame covers the whole canvas, so it replaces the previous one instead of being blended over it
    private static void SetFrameMetadata(ImageFrameMetadata frameMeta, ExportOptions options, double durationSec, long webpDelayMilliseconds, long gifDelayCentiseconds)
    {
        switch (options.AnimatedTextureFormat)
        {
            case EAnimatedTextureFormat.Png:
                var pngFrame = frameMeta.GetPngMetadata();
                pngFrame.FrameDelay = ToPngFrameDelay(durationSec);
                pngFrame.DisposalMethod = PngDisposalMethod.RestoreToBackground;
                pngFrame.BlendMethod = PngBlendMethod.Source;
                break;
            case EAnimatedTextureFormat.Webp:
                var webpFrame = frameMeta.GetWebpMetadata();
                webpFrame.FrameDelay = (uint) Math.Clamp(webpDelayMilliseconds, 1, 0xFFFFFF);
                webpFrame.DisposalMethod = WebpDisposalMethod.RestoreToBackground;
                webpFrame.BlendMethod = WebpBlendMethod.Source;
                break;
            case EAnimatedTextureFormat.Gif:
                var gifFrame = frameMeta.GetGifMetadata();
                gifFrame.FrameDelay = (int) Math.Clamp(gifDelayCentiseconds, 1, ushort.MaxValue);
                gifFrame.DisposalMethod = GifDisposalMethod.RestoreToBackground;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options), options.AnimatedTextureFormat, "Unsupported animated texture format");
        }
    }

    // APNG stores the delay as a fraction of two 16-bit numbers. The first continued fraction convergent within float
    // precision keeps rates like 29.97 at 100/2997, otherwise the closest one that still fits is used.
    private static Rational ToPngFrameDelay(double seconds)
    {
        var (numerator, denominator, previousNumerator, previousDenominator) = (1L, 0L, 0L, 1L);
        var remainder = seconds;
        while (true)
        {
            var whole = (long) Math.Floor(remainder);
            var (nextNumerator, nextDenominator) = (whole * numerator + previousNumerator, whole * denominator + previousDenominator);
            if (nextNumerator > ushort.MaxValue || nextDenominator > ushort.MaxValue)
                break;

            (previousNumerator, previousDenominator, numerator, denominator) = (numerator, denominator, nextNumerator, nextDenominator);
            if (Math.Abs((double) numerator / denominator - seconds) <= seconds * 1e-6 || remainder - whole < 1e-9)
                break;

            remainder = 1 / (remainder - whole);
        }

        return denominator == 0 ? new Rational(ushort.MaxValue, 1) : new Rational((uint) numerator, (uint) denominator);
    }
}
