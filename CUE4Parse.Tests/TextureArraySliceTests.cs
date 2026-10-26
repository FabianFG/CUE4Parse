using AssetRipper.TextureDecoder.Bc;
using AssetRipper.TextureDecoder.Rgb.Formats;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse_Conversion.Textures;
using CUE4Parse_Conversion.Textures.BC;

namespace CUE4Parse.Tests;

public class TextureArraySliceTests
{
    private delegate void ReferenceDecoder(byte[] slice, int sizeX, int sizeY, out byte[] output);

    private static readonly Dictionary<string, (int BlockBytes, BCDecoder.LayerDecoder Decode, ReferenceDecoder Reference)> Formats = new()
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

        var decoded = TextureDecoder.DecodeSlices(bytes, sizeX, sizeY, sizeZ, 4, 4, blockBytes, decode);

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
        Assert.Equal(expected, TextureDecoder.DecodeSlices(bytes, sizeX, sizeY, 1, 4, 4, blockBytes, decode));
    }

    [Theory]
    [InlineData("BC1")]
    [InlineData("BC4")]
    [InlineData("BC7")]
    public void TruncatedArraysKeepTheirCompleteSlices(string format)
    {
        var (blockBytes, decode, reference) = Formats[format];
        var sliceBytes = SliceBytes(4, 4, blockBytes);
        var bytes = new byte[sliceBytes * 3 - 1];
        new Random(blockBytes).NextBytes(bytes);

        var decoded = TextureDecoder.DecodeSlices(bytes, 4, 4, 3, 4, 4, blockBytes, decode);

        // Only the two complete slices come back, so DecodeTextureArray leaves the third one null
        const int pixelBytes = 4 * 4 * 4;
        Assert.Equal(pixelBytes * 2, decoded.Length);
        for (var z = 0; z < 2; z++)
        {
            reference(bytes[(z * sliceBytes)..((z + 1) * sliceBytes)], 4, 4, out var expected);
            Assert.Equal(expected, decoded[(z * pixelBytes)..((z + 1) * pixelBytes)]);
        }
    }

    [Fact]
    public void ArraysWithoutACompleteSliceThrow()
    {
        var (blockBytes, decode, _) = Formats["BC1"];
        var bytes = new byte[SliceBytes(16, 16, blockBytes) - 1];

        Assert.Throws<ParserException>(() => TextureDecoder.DecodeSlices(bytes, 16, 16, 3, 4, 4, blockBytes, decode));
    }

    [Fact]
    public void EmptySlicesDecodeToNothing()
    {
        var (blockBytes, decode, _) = Formats["BC1"];
        Assert.Empty(TextureDecoder.DecodeSlices([], 0, 16, 3, 4, 4, blockBytes, decode));
    }

    public static TheoryData<EPixelFormat, int, int> WiringCases()
    {
        var data = new TheoryData<EPixelFormat, int, int>();
        EPixelFormat[] formats =
        [
            EPixelFormat.PF_DXT1, EPixelFormat.PF_DXT3, EPixelFormat.PF_DXT5, EPixelFormat.PF_BC4, EPixelFormat.PF_BC5,
            EPixelFormat.PF_BC6H, EPixelFormat.PF_BC6H_Signed, EPixelFormat.PF_BC7,
            EPixelFormat.PF_ETC1, EPixelFormat.PF_ETC2_RGB, EPixelFormat.PF_ETC2_RGBA, EPixelFormat.PF_ETC2_R11_EAC, EPixelFormat.PF_ETC2_RG11_EAC,
            EPixelFormat.PF_ASTC_4x4, EPixelFormat.PF_ASTC_8x5, EPixelFormat.PF_ASTC_12x12,
        ];
        foreach (var format in formats)
        {
            foreach (var (x, y) in new[] { (2, 2), (12, 20) })
            {
                data.Add(format, x, y);
            }
        }
        return data;
    }

    // Goes through the real format switch, so a wrong block size or decoder at a call site shows up as a slice that
    // doesn't match decoding that slice alone
    [Theory]
    [MemberData(nameof(WiringCases))]
    public void DecodeBytesSplitsArraysAtTheFormatsSliceSize(EPixelFormat format, int sizeX, int sizeY)
    {
        TextureDecoder.UseAssetRipperTextureDecoder = true;
        var info = PixelFormatUtils.PixelFormats[format];
        const int sizeZ = 3;
        var sliceBytes = (sizeX + info.BlockSizeX - 1) / info.BlockSizeX * ((sizeY + info.BlockSizeY - 1) / info.BlockSizeY) * info.BlockBytes;
        var bytes = new byte[sliceBytes * sizeZ];
        new Random((int) format * 97 + sizeX).NextBytes(bytes);
        if (format.ToString().StartsWith("PF_ASTC"))
        {
            // AssetRipper's ASTC decoder throws on random blocks, so use constant colour (void extent) blocks with random colours
            for (var block = 0; block < bytes.Length; block += 16)
            {
                ReadOnlySpan<byte> voidExtent = [0xFC, 0xFD, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
                voidExtent.CopyTo(bytes.AsSpan(block));
            }
        }

        TextureDecoder.DecodeBytes(bytes, sizeX, sizeY, sizeZ, info, false, out var decoded, out var colorType);

        var pixelBytes = sizeX * sizeY * 4;
        Assert.Equal(pixelBytes * sizeZ, decoded.Length);
        for (var z = 0; z < sizeZ; z++)
        {
            TextureDecoder.DecodeBytes(bytes[(z * sliceBytes)..((z + 1) * sliceBytes)], sizeX, sizeY, 1, info, false, out var expected, out var sliceColorType);
            Assert.Equal(colorType, sliceColorType);
            Assert.Equal(expected, decoded[(z * pixelBytes)..((z + 1) * pixelBytes)]);
        }
    }
}
