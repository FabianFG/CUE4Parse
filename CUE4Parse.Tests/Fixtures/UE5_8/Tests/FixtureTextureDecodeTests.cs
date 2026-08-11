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
            AssertCookedPlatformData(texture, expectation);
            AssertDecodedBitmap(texture, expectation);

            if (expectation.MipCount > 1)
                AssertEveryMipDecodes(texture, expectation.Width, expectation.MipCount);
        }
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void VirtualTextureFixtureParsesAndDecodesTiles(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var texture = LoadTexture(provider, "T_Virtual");
        var virtualData = Assert.IsType<FVirtualTextureBuiltData>(texture.PlatformData.VTData);

        Assert.True(virtualData.IsInitialized());
        Assert.Equal((uint) 1, virtualData.NumLayers);
        Assert.Equal((uint) 512, virtualData.Width);
        Assert.Equal((uint) 512, virtualData.Height);
        Assert.True(virtualData.NumMips > 1);
        Assert.True(virtualData.TileSize > 0);
        Assert.Equal([EPixelFormat.PF_DXT1], virtualData.LayerTypes);
        Assert.NotEmpty(virtualData.Chunks);
        Assert.All(virtualData.Chunks, chunk =>
            Assert.NotEmpty(Assert.IsType<byte[]>(chunk.BulkData.Data)));

        var expectation = new TextureExpectation(
            "T_Virtual", "streaming.png", 512, 512,
            EPixelFormat.PF_DXT1, EPixelFormat.PF_R8G8B8A8, SKColorType.Rgba8888,
            (int) virtualData.NumMips, 1.5, 40);
        AssertDecodedBitmap(texture, expectation);
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void UdimVirtualTexturePreservesBlocksAndDecodesComposite(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);
        var texture = LoadTexture(provider, "T_UDIM");
        var virtualData = Assert.IsType<FVirtualTextureBuiltData>(texture.PlatformData.VTData);

        Assert.True(virtualData.IsInitialized());
        Assert.Equal((uint) 1, virtualData.NumLayers);
        Assert.Equal((uint) 2, virtualData.WidthInBlocks);
        Assert.Equal((uint) 2, virtualData.HeightInBlocks);
        Assert.Equal((uint) 256, virtualData.Width);
        Assert.Equal((uint) 256, virtualData.Height);
        Assert.Equal([EPixelFormat.PF_DXT1], virtualData.LayerTypes);
        Assert.NotEmpty(virtualData.Chunks);

        var decoded = texture.Decode();
        Assert.NotNull(decoded);
        using var actual = decoded.ToSkBitmap();
        using var expected = CreateExpectedUdimBitmap();
        Assert.Equal((256, 256), (actual.Width, actual.Height));
        var metrics = Measure(actual, expected);
        for (var channel = 0; channel < ChannelCount; channel++)
        {
            Assert.True(metrics[channel].Mean <= 2.0,
                $"T_UDIM channel {ChannelName(channel)} mean error is {metrics[channel].Mean:F3}. {metrics}");
            Assert.True(metrics[channel].Maximum <= 70,
                $"T_UDIM channel {ChannelName(channel)} maximum error is {metrics[channel].Maximum}. {metrics}");
        }
    }

    [Theory]
    [InlineData(FixtureSerialization.Tagged)]
    [InlineData(FixtureSerialization.Unversioned)]
    public void SlicedTextureFixturesDecodeEveryGeneratedPixel(FixtureSerialization serialization)
    {
        using var provider = CreateMountedIoStoreProvider(serialization);

        var cube = LoadExport<UTextureCube>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Textures/T_Cube.uasset",
            "T_Cube");
        Assert.Equal(EPixelFormat.PF_B8G8R8A8, cube.Format);
        Assert.Equal(6, cube.PlatformData.GetNumSlices());
        var decodedCube = cube.Decode();
        Assert.NotNull(decodedCube);
        using (var bitmap = decodedCube.ToSkBitmap())
        {
            Assert.Equal((16, 96), (bitmap.Width, bitmap.Height));
            for (var slice = 0; slice < 6; slice++)
                AssertGeneratedSlice(bitmap, 16, 16, slice, slice * 16);
        }

        var array = LoadExport<UTexture2DArray>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Textures/T_Array.uasset",
            "T_Array");
        Assert.Equal(EPixelFormat.PF_B8G8R8A8, array.Format);
        Assert.Equal(4, array.PlatformData.GetNumSlices());
        var slices = array.DecodeTextureArray();
        Assert.NotNull(slices);
        Assert.Equal(4, slices.Length);
        for (var slice = 0; slice < slices.Length; slice++)
        {
            using var bitmap = slices[slice].ToSkBitmap();
            Assert.Equal((16, 16), (bitmap.Width, bitmap.Height));
            AssertGeneratedSlice(bitmap, 16, 16, slice);
        }

        var volume = LoadExport<UVolumeTexture>(
            provider,
            "CUE4ParseFixtures/Content/Fixtures/Textures/T_Volume.uasset",
            "T_Volume");
        Assert.Equal(EPixelFormat.PF_B8G8R8A8, volume.Format);
        Assert.Equal(4, volume.PlatformData.GetNumSlices());
        var firstVolumeMip = volume.GetFirstMip();
        Assert.NotNull(firstVolumeMip);
        Assert.Equal(4, firstVolumeMip.SizeZ);
        var decodedVolume = volume.Decode();
        Assert.NotNull(decodedVolume);
        using (var bitmap = decodedVolume.ToSkBitmap())
        {
            Assert.Equal((8, 32), (bitmap.Width, bitmap.Height));
            for (var slice = 0; slice < 4; slice++)
                AssertGeneratedSlice(bitmap, 8, 8, slice, slice * 8);
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

    private static SKBitmap CreateExpectedUdimBitmap()
    {
        var result = new SKBitmap(256, 256, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        using var canvas = new SKCanvas(result);
        Draw("udim.1001.png", 0, 0);
        Draw("udim.1002.png", 128, 0);
        Draw("udim.1011.png", 0, 128);
        Draw("udim.1012.png", 128, 128);
        return result;

        void Draw(string source, int x, int y)
        {
            using var tile = SKBitmap.Decode(FixturePath("SourceTextures", source));
            Assert.NotNull(tile);
            canvas.DrawBitmap(tile, x, y);
        }
    }

    private static void AssertDecodedBitmap(UTexture2D texture, TextureExpectation expectation)
    {
        var decoded = texture.Decode();
        Assert.NotNull(decoded);
        using var actual = decoded.ToSkBitmap();
        using var expected = CreateExpectedBitmap(expectation);

        Assert.Equal(expectation.DecodedFormat, decoded.PixelFormat);
        Assert.Equal(expectation.SkiaColorType, actual.ColorType);
        Assert.Equal(expectation.Width, actual.Width);
        Assert.Equal(expectation.Height, actual.Height);
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

    private static void AssertGeneratedSlice(SKBitmap bitmap, int width, int height, int slice, int yOffset = 0)
    {
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                Assert.Equal(MakeFixtureColor(x, y, slice), bitmap.GetPixel(x, y + yOffset));
    }

    private static SKColor MakeFixtureColor(int x, int y, int slice) => new(
        (byte) ((x * 17 + slice * 41) & 0xff),
        (byte) ((y * 29 + slice * 67) & 0xff),
        (byte) (((x + y) * 11 + slice * 97) & 0xff),
        (byte) (255 - ((x * 3 + y * 5 + slice * 7) & 0x7f)));

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

    private static void AssertEveryMipDecodes(UTexture2D texture, int baseSize, int mipCount)
    {
        for (var mipIndex = 0; mipIndex < mipCount; mipIndex++)
        {
            var expectedDimension = Math.Max(1, baseSize >> mipIndex);
            var decoded = texture.DecodeMip(mipIndex);
            Assert.NotNull(decoded);
            Assert.Equal(expectedDimension, decoded.Width);
            Assert.Equal(expectedDimension, decoded.Height);
            Assert.Equal(decoded.Width * decoded.Height * 4, decoded.Data.Length);

            using var bitmap = decoded.ToSkBitmap();
            Assert.Equal(expectedDimension, bitmap.Width);
            Assert.Equal(expectedDimension, bitmap.Height);
            for (var y = 0; y < bitmap.Height; y++)
                for (var x = 0; x < bitmap.Width; x++)
                    Assert.Equal(255, bitmap.GetPixel(x, y).Alpha);
        }
    }

    private static void AssertCookedPlatformData(UTexture2D texture, TextureExpectation expectation)
    {
        Assert.Equal(expectation.CookedFormat, texture.Format);
        Assert.Equal(expectation.Width, texture.PlatformData.SizeX);
        Assert.Equal(expectation.Height, texture.PlatformData.SizeY);
        Assert.Equal(expectation.MipCount, texture.PlatformData.Mips.Length);

        for (var mipIndex = 0; mipIndex < expectation.MipCount; mipIndex++)
        {
            var expectedWidth = Math.Max(1, expectation.Width >> mipIndex);
            var expectedHeight = Math.Max(1, expectation.Height >> mipIndex);
            var mip = texture.GetMip(mipIndex);
            Assert.NotNull(mip);
            Assert.Equal(expectedWidth, mip.SizeX);
            Assert.Equal(expectedHeight, mip.SizeY);
            Assert.Equal(1, mip.SizeZ);
            Assert.NotEmpty(Assert.IsType<byte[]>(mip.BulkData?.Data));
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
