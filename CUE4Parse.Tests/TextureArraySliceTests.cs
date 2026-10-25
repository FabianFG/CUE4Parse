using AssetRipper.TextureDecoder.Bc;
using AssetRipper.TextureDecoder.Rgb.Formats;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse_Conversion.Textures;

namespace CUE4Parse.Tests;

public class TextureArraySliceTests
{
    private delegate void ReferenceDecoder(byte[] slice, int sizeX, int sizeY, out byte[] output);

    private static readonly Dictionary<string, (int BlockBytes, TextureDecoder.SliceDecoder Decode, ReferenceDecoder Reference)> Formats = new()
    {
        ["BC1"] = (8, static (s, x, y, o) => Bc1.Decompress<ColorRGBA<byte>, byte>(s, x, y, o), static (byte[] s, int x, int y, out byte[] o) => Bc1.Decompress<ColorRGBA<byte>, byte>(s, x, y, out o)),
        ["BC2"] = (16, static (s, x, y, o) => Bc2.Decompress<ColorRGBA<byte>, byte>(s, x, y, o), static (byte[] s, int x, int y, out byte[] o) => Bc2.Decompress<ColorRGBA<byte>, byte>(s, x, y, out o)),
        ["BC3"] = (16, static (s, x, y, o) => Bc3.Decompress<ColorRGBA<byte>, byte>(s, x, y, o), static (byte[] s, int x, int y, out byte[] o) => Bc3.Decompress<ColorRGBA<byte>, byte>(s, x, y, out o)),
        ["BC4"] = (8, static (s, x, y, o) => Bc4.Decompress<ColorBGRA<byte>, byte>(s, x, y, o), static (byte[] s, int x, int y, out byte[] o) => Bc4.Decompress<ColorBGRA<byte>, byte>(s, x, y, out o)),
        ["BC5"] = (16, static (s, x, y, o) => Bc5.Decompress<ColorBGRA<byte>, byte>(s, x, y, o), static (byte[] s, int x, int y, out byte[] o) => Bc5.Decompress<ColorBGRA<byte>, byte>(s, x, y, out o)),
        ["BC6H"] = (16, static (s, x, y, o) => Bc6h.Decompress<ColorRGBA<byte>, byte>(s, x, y, false, o), static (byte[] s, int x, int y, out byte[] o) => Bc6h.Decompress<ColorRGBA<byte>, byte>(s, x, y, false, out o)),
        ["BC7"] = (16, static (s, x, y, o) => Bc7.Decompress<ColorRGBA<byte>, byte>(s, x, y, o), static (byte[] s, int x, int y, out byte[] o) => Bc7.Decompress<ColorRGBA<byte>, byte>(s, x, y, out o)),
    };

    public static TheoryData<string, int, int> Cases()
    {
        var data = new TheoryData<string, int, int>();
        foreach (var format in Formats.Keys)
        {
            // 1x1 and 2x2 are the small mips where slices stop lining up with whole block rows
            foreach (var (x, y) in new[] { (1, 1), (2, 2), (4, 4), (12, 20) })
            {
                data.Add(format, x, y);
            }
        }
        return data;
    }

    private static int SliceBytes(int sizeX, int sizeY, int blockBytes) => (sizeX + 3) / 4 * ((sizeY + 3) / 4) * blockBytes;

    [Theory]
    [MemberData(nameof(Cases))]
    public void EverySliceMatchesAnIndependentDecode(string format, int sizeX, int sizeY)
    {
        const int sizeZ = 3;
        var (blockBytes, decode, reference) = Formats[format];
        var sliceBytes = SliceBytes(sizeX, sizeY, blockBytes);
        var bytes = new byte[sliceBytes * sizeZ];
        new Random(sizeX * 31 + sizeY).NextBytes(bytes);

        var decoded = TextureDecoder.DecodeSlices(bytes, sizeX, sizeY, sizeZ, blockBytes, decode);

        var pixelBytes = sizeX * sizeY * 4;
        Assert.Equal(pixelBytes * sizeZ, decoded.Length);
        for (var z = 0; z < sizeZ; z++)
        {
            reference(bytes[(z * sliceBytes)..((z + 1) * sliceBytes)], sizeX, sizeY, out var expected);
            Assert.Equal(expected, decoded[(z * pixelBytes)..((z + 1) * pixelBytes)]);
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void SingleImagesDecodeLikeBefore(string format, int sizeX, int sizeY)
    {
        var (blockBytes, decode, reference) = Formats[format];
        var bytes = new byte[SliceBytes(sizeX, sizeY, blockBytes)];
        new Random(sizeX * 17 + sizeY).NextBytes(bytes);

        reference(bytes, sizeX, sizeY, out var expected);
        Assert.Equal(expected, TextureDecoder.DecodeSlices(bytes, sizeX, sizeY, 1, blockBytes, decode));
    }

    [Theory]
    [InlineData("BC1")]
    [InlineData("BC4")]
    [InlineData("BC7")]
    public void TruncatedArraysAreRejected(string format)
    {
        var (blockBytes, decode, _) = Formats[format];
        var bytes = new byte[SliceBytes(4, 4, blockBytes) * 3 - 1];

        Assert.Throws<ParserException>(() => TextureDecoder.DecodeSlices(bytes, 4, 4, 3, blockBytes, decode));
    }
}
