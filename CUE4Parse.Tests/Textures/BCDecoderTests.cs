using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures.BC;

namespace CUE4Parse.Tests.Textures;

public class BCDecoderTests
{
    // Random blocks reach every BC6H and BC7 mode, including reserved ones, and the c0 <= c1 palettes of BC1 to BC5
    [Theory]
    [InlineData(EPixelFormat.PF_DXT1)]
    [InlineData(EPixelFormat.PF_DXT3)]
    [InlineData(EPixelFormat.PF_DXT5)]
    [InlineData(EPixelFormat.PF_BC4)]
    [InlineData(EPixelFormat.PF_BC5)]
    [InlineData(EPixelFormat.PF_BC6H)]
    [InlineData(EPixelFormat.PF_BC7)]
    public void RandomBlocksMatchAssetRipperDecoders(EPixelFormat format)
    {
        var random = new Random(1000 + (int) format);
        (int Width, int Height, int Runs)[] sizes = [(128, 128, 4), (6, 6, 20), (1, 1, 20), (13, 7, 20)];
        foreach (var (width, height, runs) in sizes)
        {
            for (var run = 0; run < runs; run++)
            {
                var data = new byte[(width + 3) / 4 * ((height + 3) / 4) * BCReferenceDecoder.GetBlockSize(format)];
                random.NextBytes(data);
                BCReferenceDecoder.AssertMatches(format, data, width, height, $"{format} {width}x{height} run {run}");
            }
        }
    }

    [Fact]
    public void IntegerNormalZMatchesFloatReconstruction()
    {
        for (var x = 0; x <= byte.MaxValue; x++)
            for (var y = 0; y <= byte.MaxValue; y++)
                Assert.Equal(BCReferenceDecoder.GetZNormal((byte) x, (byte) y), BCDecoder.GetZNormal((byte) x, (byte) y));
    }
}
