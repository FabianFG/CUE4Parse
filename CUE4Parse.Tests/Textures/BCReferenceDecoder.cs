using AssetRipper.TextureDecoder.Bc;
using AssetRipper.TextureDecoder.Rgb.Formats;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse_Conversion.Textures.BC;

namespace CUE4Parse.Tests.Textures;

/// <summary>Compares CUE4Parse's BC decoders with the AssetRipper decoders they replaced.</summary>
internal static class BCReferenceDecoder
{
    public static bool IsSupported(EPixelFormat format) => format is EPixelFormat.PF_DXT1 or EPixelFormat.PF_DXT3 or
        EPixelFormat.PF_DXT5 or EPixelFormat.PF_BC4 or EPixelFormat.PF_BC5 or EPixelFormat.PF_BC6H or EPixelFormat.PF_BC7;

    public static int GetBlockSize(EPixelFormat format) => format is EPixelFormat.PF_DXT1 or EPixelFormat.PF_BC4 ? 8 : 16;

    // Both decoders follow the format specs, but round interpolated BC1 to BC5 palette entries differently.
    // BC6H and BC7 have no interpolation leeway and must match exactly.
    public static void AssertMatches(EPixelFormat format, byte[] data, int width, int height, string label)
    {
        var pixels = width * height;
        byte[] actual, expected;
        switch (format)
        {
            case EPixelFormat.PF_DXT1:
                actual = BCDecoder.BC1(data, width, height, 1);
                Bc1.Decompress<ColorRGBA<byte>, byte>(data, width, height, out expected);
                AssertChannels(actual, expected, pixels, label, (0, 2), (1, 2), (2, 2), (3, 0));
                break;
            case EPixelFormat.PF_DXT3:
                actual = BCDecoder.BC2(data, width, height, 1);
                Bc2.Decompress<ColorRGBA<byte>, byte>(data, width, height, out expected);
                AssertChannels(actual, expected, pixels, label, (0, 2), (1, 2), (2, 2), (3, 0));
                break;
            case EPixelFormat.PF_DXT5:
                actual = BCDecoder.BC3(data, width, height, 1);
                Bc3.Decompress<ColorRGBA<byte>, byte>(data, width, height, out expected);
                AssertChannels(actual, expected, pixels, label, (0, 2), (1, 2), (2, 2), (3, 1));
                break;
            case EPixelFormat.PF_BC4:
                // AssetRipper only writes red, CUE4Parse writes the value as grey
                actual = BCDecoder.BC4(data, width, height, 1);
                Bc4.Decompress<ColorBGRA<byte>, byte>(data, width, height, out expected);
                AssertChannels(actual, expected, pixels, label, (2, 1));
                for (var i = 0; i < pixels; i++)
                {
                    var value = actual[i * 4 + 2];
                    if (actual[i * 4] != value || actual[i * 4 + 1] != value || actual[i * 4 + 3] != byte.MaxValue)
                        Assert.Fail($"{label}: BC4 pixel {i} is {Format(actual, i)}, expected grey {value} with opaque alpha.");
                }
                break;
            case EPixelFormat.PF_BC5:
                actual = BCDecoder.BC5(data, width, height, 1);
                Bc5.Decompress<ColorBGRA<byte>, byte>(data, width, height, out expected);
                AssertChannels(actual, expected, pixels, label, (2, 1), (1, 1));
                for (var i = 0; i < pixels; i++)
                {
                    var z = GetZNormal(actual[i * 4 + 2], actual[i * 4 + 1]);
                    if (actual[i * 4] != z || actual[i * 4 + 3] != byte.MaxValue)
                        Assert.Fail($"{label}: BC5 pixel {i} is {Format(actual, i)}, expected Z {z} with opaque alpha.");
                }
                break;
            case EPixelFormat.PF_BC6H:
                actual = BCDecoder.BC6H(data, width, height, 1);
                Bc6h.Decompress<ColorRGBA<byte>, byte>(data, width, height, false, out expected);
                AssertChannels(actual, expected, pixels, label, (0, 0), (1, 0), (2, 0), (3, 0));
                break;
            case EPixelFormat.PF_BC7:
                actual = BCDecoder.BC7(data, width, height, 1);
                Bc7.Decompress<ColorRGBA<byte>, byte>(data, width, height, out expected);
                AssertChannels(actual, expected, pixels, label, (0, 0), (1, 0), (2, 0), (3, 0));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }

    /// <summary>The float normal Z reconstruction that <see cref="BCDecoder.GetZNormal"/> approximates with integers.</summary>
    public static byte GetZNormal(byte x, byte y)
    {
        const float scale = 2.0f / 255.0f;
        var normalX = x * scale - 1;
        var normalY = y * scale - 1;
        var normalZ = MathF.Sqrt(MathF.Max(0, 1 - normalX * normalX - normalY * normalY));
        return (byte) (MathF.Min(normalZ, 1) * 127 + 128);
    }

    private static void AssertChannels(byte[] actual, byte[] expected, int pixels, string label, params (int Offset, int Tolerance)[] channels)
    {
        Assert.Equal(pixels * 4, actual.Length);
        Assert.Equal(pixels * 4, expected.Length);
        foreach (var (offset, tolerance) in channels)
        {
            for (var i = 0; i < pixels; i++)
            {
                var difference = Math.Abs(actual[i * 4 + offset] - expected[i * 4 + offset]);
                if (difference > tolerance)
                    Assert.Fail($"{label}: byte {offset} of pixel {i} differs by {difference}, more than {tolerance}. " +
                                $"CUE4Parse {Format(actual, i)}, AssetRipper {Format(expected, i)}.");
            }
        }
    }

    private static string Format(byte[] pixels, int index) =>
        $"[{pixels[index * 4]}, {pixels[index * 4 + 1]}, {pixels[index * 4 + 2]}, {pixels[index * 4 + 3]}]";
}
