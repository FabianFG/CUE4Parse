using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures;
using SkiaSharp;
using static CUE4Parse.Tests.Fixtures.UE5_8.FixtureTestUtilities;

namespace CUE4Parse.Tests.Fixtures.UE5_8;

public class FixtureTextureDecodeTests
{
    private const int ChannelCount = 4;

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void TextureFixturesDecodeToExpectedSkiaBitmaps(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);

        foreach (var expectation in TextureExpectations)
        {
            var texture = LoadTexture(provider, expectation.Asset);
            var decoded = texture.Decode();
            Assert.NotNull(decoded);
            using var actual = decoded.ToSkBitmap();
            using var expected = CreateExpectedBitmap(expectation);

            Assert.Equal(expectation.DecodedFormat, decoded.PixelFormat);
            Assert.Equal(expectation.SkiaColorType, actual.ColorType);
            Assert.Equal(64, actual.Width);
            Assert.Equal(64, actual.Height);
            Assert.Equal(actual.Width, expected.Width);
            Assert.Equal(actual.Height, expected.Height);
            var metrics = Measure(actual, expected);
            for (var channel = 0; channel < ChannelCount; channel++)
            {
                var channelMetrics = metrics[channel];
                Assert.True(
                    channelMetrics.Mean <= expectation.MaximumMeanError,
                    $"{expectation.Asset} channel {ChannelName(channel)} mean error " +
                    $"{channelMetrics.Mean:F3} exceeds {expectation.MaximumMeanError:F3}. {metrics}");
                Assert.True(
                    channelMetrics.Maximum <= expectation.MaximumPixelError,
                    $"{expectation.Asset} channel {ChannelName(channel)} maximum error " +
                    $"{channelMetrics.Maximum} exceeds {expectation.MaximumPixelError}. {metrics}");
            }

            if (expectation.Asset == "T_Mips")
                AssertEveryMipDecodes(texture);
        }
    }

    private static SKBitmap CreateExpectedBitmap(TextureExpectation expectation)
    {
        if (expectation.Asset == "T_BC6H")
        {
            var bitmap = new SKBitmap(64, 64, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            for (var y = 0; y < bitmap.Height; y++)
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var red = ToByte(16.0f * x / (bitmap.Width - 1));
                    var green = ToByte(8.0f * y / (bitmap.Height - 1));
                    var blue = ToByte(4.0f * (x + y) / (bitmap.Width + bitmap.Height - 2));
                    bitmap.SetPixel(x, y, new SKColor(red, green, blue, 255));
                }
            return bitmap;
        }

        var path = FixturePath("SourceTextures", expectation.Source);
        var reference = SKBitmap.Decode(path);
        Assert.NotNull(reference);

        if (expectation.Asset == "T_BC4")
        {
            for (var y = 0; y < reference.Height; y++)
                for (var x = 0; x < reference.Width; x++)
                {
                    var value = reference.GetPixel(x, y).Red;
                    reference.SetPixel(x, y, new SKColor(value, 0, 0, 255));
                }
        }
        else if (expectation.Asset == "T_BC5")
        {
            for (var y = 0; y < reference.Height; y++)
                for (var x = 0; x < reference.Width; x++)
                {
                    var source = reference.GetPixel(x, y);
                    reference.SetPixel(x, y, new SKColor(source.Red, source.Green, GetZNormal(source.Red, source.Green), 255));
                }
        }

        return reference;
    }

    private static byte ToByte(float value) => (byte) Math.Clamp(value * 255.0f, 0, 255);

    private static byte GetZNormal(byte x, byte y)
    {
        const float scale = 2.0f / 255.0f;
        var normalX = x * scale - 1.0f;
        var normalY = y * scale - 1.0f;
        var normalZ = MathF.Sqrt(MathF.Max(0.0f, 1.0f - normalX * normalX - normalY * normalY));
        return (byte) (MathF.Min(normalZ, 1.0f) * 127.0f + 128.0f);
    }

    private static PixelMetrics Measure(SKBitmap actual, SKBitmap expected)
    {
        Span<long> sums = stackalloc long[ChannelCount];
        Span<int> maxima = stackalloc int[ChannelCount];
        Span<int> differences = stackalloc int[ChannelCount];
        for (var y = 0; y < actual.Height; y++)
        {
            for (var x = 0; x < actual.Width; x++)
            {
                var a = actual.GetPixel(x, y);
                var e = expected.GetPixel(x, y);
                differences[0] = Math.Abs(a.Red - e.Red);
                differences[1] = Math.Abs(a.Green - e.Green);
                differences[2] = Math.Abs(a.Blue - e.Blue);
                differences[3] = Math.Abs(a.Alpha - e.Alpha);
                for (var channel = 0; channel < differences.Length; channel++)
                {
                    sums[channel] += differences[channel];
                    maxima[channel] = Math.Max(maxima[channel], differences[channel]);
                }
            }
        }

        var pixels = actual.Width * actual.Height;
        return new PixelMetrics(
            new ChannelMetrics(sums[0] / (double) pixels, maxima[0]),
            new ChannelMetrics(sums[1] / (double) pixels, maxima[1]),
            new ChannelMetrics(sums[2] / (double) pixels, maxima[2]),
            new ChannelMetrics(sums[3] / (double) pixels, maxima[3]));
    }

    private static void AssertEveryMipDecodes(UTexture2D texture)
    {
        var expectedDimensions = ExpectedMipDimensions;
        for (var mipIndex = 0; mipIndex < expectedDimensions.Length; mipIndex++)
        {
            var decoded = texture.DecodeMip(mipIndex);
            Assert.NotNull(decoded);
            Assert.Equal(expectedDimensions[mipIndex], decoded.Width);
            Assert.Equal(expectedDimensions[mipIndex], decoded.Height);
            Assert.Equal(decoded.Width * decoded.Height * 4, decoded.Data.Length);

            using var bitmap = decoded.ToSkBitmap();
            Assert.Equal(expectedDimensions[mipIndex], bitmap.Width);
            Assert.Equal(expectedDimensions[mipIndex], bitmap.Height);
            for (var y = 0; y < bitmap.Height; y++)
                for (var x = 0; x < bitmap.Width; x++)
                    Assert.Equal(255, bitmap.GetPixel(x, y).Alpha);
        }
    }

    private static string ChannelName(int channel) => channel switch
    {
        0 => "red",
        1 => "green",
        2 => "blue",
        3 => "alpha",
        _ => throw new ArgumentOutOfRangeException(nameof(channel))
    };

    private static UTexture2D LoadTexture(DefaultFileProvider provider, string name)
    {
        var suffix = $"CUE4ParseFixtures/Content/Fixtures/Textures/{name}.uasset";
        return LoadExport<UTexture2D>(provider, suffix, name);
    }

    private readonly record struct ChannelMetrics(double Mean, int Maximum);

    private readonly record struct PixelMetrics(
        ChannelMetrics Red,
        ChannelMetrics Green,
        ChannelMetrics Blue,
        ChannelMetrics Alpha)
    {
        public ChannelMetrics this[int channel] => channel switch
        {
            0 => Red,
            1 => Green,
            2 => Blue,
            3 => Alpha,
            _ => throw new ArgumentOutOfRangeException(nameof(channel))
        };

        public override string ToString() =>
            $"mean=[{Red.Mean:F3}, {Green.Mean:F3}, {Blue.Mean:F3}, {Alpha.Mean:F3}], " +
            $"max=[{Red.Maximum}, {Green.Maximum}, {Blue.Maximum}, {Alpha.Maximum}]";
    }
}
