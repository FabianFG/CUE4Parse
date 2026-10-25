using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Textures;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace CUE4Parse.Tests;

public class PaperFlipbookTests
{
    // Every pixel stores its own coordinates, so crops and rotations can be checked pixel by pixel
    private static CTexture CoordinateTexture(int width, int height, Func<int, int, (int X, int Y)>? source = null)
    {
        var data = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var (sourceX, sourceY) = source?.Invoke(x, y) ?? (x, y);
                data[(y * width + x) * 4] = (byte) sourceX;
                data[(y * width + x) * 4 + 1] = (byte) sourceY;
                data[(y * width + x) * 4 + 3] = 255;
            }
        }
        return new CTexture(width, height, EPixelFormat.PF_R8G8B8A8, data);
    }

    private static (int X, int Y) Pixel(CTexture texture, int x, int y) =>
        (texture.Data[(y * texture.Width + x) * 4], texture.Data[(y * texture.Width + x) * 4 + 1]);

    // Vertices of a sprite rectangle as UPaperSprite::RebuildRenderData bakes them
    private static FVector4[] RenderData(float x, float y, float width, float height, float pivotX, float pivotY,
        float ppuu, int textureWidth, int textureHeight, bool rotated = false)
    {
        return new[] { (x, y), (x + width, y), (x, y + height), (x + width, y + height) }.Select(corner =>
        {
            var (pivotSpaceX, pivotSpaceY) = (corner.Item1 - pivotX, -corner.Item2 + pivotY);
            if (rotated)
                (pivotSpaceX, pivotSpaceY) = (-pivotSpaceY, pivotSpaceX);
            return new FVector4(pivotSpaceX / ppuu, pivotSpaceY / ppuu, corner.Item1 / textureWidth, corner.Item2 / textureHeight);
        }).ToArray();
    }

    [Fact]
    public void SpritesAreCutFromSmallerCookedMips()
    {
        var sheet = CoordinateTexture(32, 32);
        var sprite = new UPaperSprite
        {
            BakedSourceUV = new FVector2D(32, 0),
            BakedSourceDimension = new FVector2D(32, 32),
            PixelsPerUnrealUnit = 2.56f,
            BakedRenderData = RenderData(32, 0, 32, 32, 48, 16, 2.56f, 64, 64),
        };

        var decoded = sprite.Decode(sheet, new FIntPoint(64, 64), out var pivot);

        Assert.Equal((16, 16), (decoded.Width, decoded.Height));
        Assert.Equal((16, 0), Pixel(decoded, 0, 0));
        Assert.Equal((31, 15), Pixel(decoded, 15, 15));
        Assert.Equal(new FIntPoint(8, 8), pivot);
    }

    [Fact]
    public void RotatedSpritesAreTurnedUpright()
    {
        // The upright sprite is 3x2, the atlas holds it as 2x3 turned clockwise
        var atlas = CoordinateTexture(2, 3, (x, y) => (y, 1 - x));
        var sprite = new UPaperSprite
        {
            BakedSourceUV = new FVector2D(0, 0),
            BakedSourceDimension = new FVector2D(2, 3),
            PixelsPerUnrealUnit = 1,
            BakedRenderData = RenderData(0, 0, 2, 3, 1, 1.5f, 1, 2, 3, rotated: true),
        };

        var decoded = sprite.Decode(atlas, new FIntPoint(2, 3), out var pivot);

        Assert.Equal((3, 2), (decoded.Width, decoded.Height));
        for (var y = 0; y < 2; y++)
            for (var x = 0; x < 3; x++)
                Assert.Equal((x, y), Pixel(decoded, x, y));
        Assert.Equal(new FIntPoint(2, 1), pivot);
    }

    [Fact]
    public void CropRegionsStayInsideTheTexture()
    {
        var sprite = new UPaperSprite
        {
            BakedSourceUV = new FVector2D(0, 0),
            BakedSourceDimension = new FVector2D(17, 16),
            PixelsPerUnrealUnit = 1,
            BakedRenderData = RenderData(0, 0, 17, 16, 8, 8, 1, 16, 16),
        };

        var decoded = sprite.Decode(CoordinateTexture(16, 16), new FIntPoint(16, 16), out _);

        Assert.Equal((16, 16), (decoded.Width, decoded.Height));
    }

    [Fact]
    public void FractionalSheetCellsKeepTheirSizeAndPivot()
    {
        // A 9 pixel sheet cut into four cells of 2.25 pixels with centered pivots
        var sheet = CoordinateTexture(9, 1);
        var crops = Enumerable.Range(0, 4).Select(cell =>
        {
            var u = cell * 2.25f;
            var sprite = new UPaperSprite
            {
                BakedSourceUV = new FVector2D(u, 0),
                BakedSourceDimension = new FVector2D(2.25f, 1),
                PixelsPerUnrealUnit = 1,
                BakedRenderData = RenderData(u, 0, 2.25f, 1, u + 1.125f, 0.5f, 1, 9, 1),
            };
            var decoded = sprite.Decode(sheet, new FIntPoint(9, 1), out var pivot);
            return (decoded.Width, First: Pixel(decoded, 0, 0).X, pivot.X);
        }).ToArray();

        Assert.Equal([(2, 0, 1), (2, 2, 1), (2, 5, 1), (2, 7, 1)], crops);
    }

    private static CFlipbook TwoFrames(float framesPerSecond)
    {
        var red = new CTexture(4, 4, EPixelFormat.PF_R8G8B8A8, Enumerable.Repeat(new byte[] { 255, 0, 0, 255 }, 16).SelectMany(p => p).ToArray());
        var green = new CTexture(2, 2, EPixelFormat.PF_R8G8B8A8, Enumerable.Repeat(new byte[] { 0, 255, 0, 255 }, 4).SelectMany(p => p).ToArray());
        return new CFlipbook([new CFlipbookFrame(red), new CFlipbookFrame(green, 7, new FIntPoint(1, 1))], framesPerSecond);
    }

    [Theory]
    [InlineData(ETextureFormat.Png)]
    [InlineData(ETextureFormat.Webp)]
    public void FramesReplaceThePreviousFrame(ETextureFormat format)
    {
        using var image = Image.Load<Rgba32>(TwoFrames(15).Encode(format, out _));

        Assert.Equal(2, image.Frames.Count);
        Assert.Equal(new Rgba32(0, 255, 0, 255), image.Frames[1][1, 1]);
        Assert.Equal(0, image.Frames[1][0, 0].A);
    }

    [Fact]
    public void PngFrameDelaysFitIntoSixteenBits()
    {
        using var image = Image.Load<Rgba32>(TwoFrames(59.94f).Encode(ETextureFormat.Png, out _));

        var delays = image.Frames.Select(frame => frame.Metadata.GetPngMetadata().FrameDelay).ToArray();
        Assert.Equal(1 / 59.94, delays[0].ToDouble(), 1e-6);
        Assert.Equal(7 / 59.94, delays[1].ToDouble(), 1e-6);
    }

    [Theory]
    [InlineData(ETextureFormat.Png)]
    [InlineData(ETextureFormat.Webp)]
    public void FlipbooksWithoutFrameRateShowTheirFirstFrame(ETextureFormat format)
    {
        var flipbook = TwoFrames(0);

        using var image = Image.Load<Rgba32>(flipbook.Encode(format, out _));

        Assert.Single(image.Frames);
        Assert.Equal(new Rgba32(255, 0, 0, 255), image[0, 0]);
        Assert.Equal(0, flipbook.Duration);
        Assert.Equal(0, new UPaperFlipbook { FramesPerSecond = 0, KeyFrames = [new FPaperFlipbookKeyFrame(null, 3)] }.GetTotalDuration());
    }

    [Theory]
    [InlineData(ETextureFormat.Jpeg)]
    [InlineData(ETextureFormat.Tga)]
    public void FormatsWithoutAnimationFallBackToPng(ETextureFormat format)
    {
        var data = TwoFrames(15).Encode(format, out var ext);

        Assert.Equal("png", ext);
        using var image = Image.Load<Rgba32>(data);
        Assert.Equal(2, image.Frames.Count);
    }
}
