using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

using System.Buffers;

using K4os.Compression.LZ4;

using OffiUtils;

using OodleDotNet;

using OodleSharp;

using ZlibngDotNet;

using ZstdSharp;

namespace CUE4Parse.Compression;

public static class Compression
{
    public const int LOADING_COMPRESSION_CHUNK_SIZE = 131072;

    public static IDecompressor Decompressor => _decompressor;

    private static IDecompressor _decompressor = DecompressorBuilder.Default
        .Add(CompressionAlgorithm.Oodle, OodleDecompressor.TryDecompress)
        .Add(CompressionAlgorithm.LZ4, static (source, destination, out written)
            => (written = LZ4Codec.Decode(source, destination)) > 0, replace: true)
        .Add(CompressionAlgorithm.Zstd, static (source, destination, out written) =>
        {
            using var decompressor = new Decompressor();
            return decompressor.TryUnwrap(source, destination, out written);
        }, replace: true)
        .Build();

    public static void UseNativeOodle(Oodle oodle)
    {
        _decompressor = new DecompressorBuilder()
            .AddRange(_decompressor, true)
            .Add(CompressionAlgorithm.Oodle, oodle, static (oodle, source, destination, out written)
                => (written = (int) oodle.Decompress(source, destination)) > 0, replace: true)
            .Build();
    }

    public static void UseNativeZlib(Zlibng zlib)
    {
        _decompressor = new DecompressorBuilder()
            .AddRange(_decompressor, true)
            .Add(CompressionAlgorithm.Zlib, zlib, static (zlib, source, destination, out written)
                => zlib.Uncompress(destination, source, out written) == ZlibngCompressionResult.Ok, replace: true)
            .Build();
    }
    
    public static void UseLZO(DecompressDelegate decompressor)
    {
        _decompressor = new DecompressorBuilder()
            .AddRange(_decompressor, true)
            .Add(CompressionAlgorithm.LZO, decompressor, replace: true)
            .Build();
    }

    public static byte[] Decompress(byte[] compressed, int uncompressedSize, CompressionMethod method, FArchive? reader = null)
        => Decompress(compressed, 0, compressed.Length, uncompressedSize, method, reader);

    public static byte[] Decompress(byte[] compressed, int compressedOffset, int compressedCount, int uncompressedSize, CompressionMethod method, FArchive? reader = null)
    {
        var uncompressed = new byte[uncompressedSize];
        Decompress(compressed, compressedOffset, compressedCount, uncompressed, 0, uncompressedSize, method);
        return uncompressed;
    }

    public static void Decompress(
        byte[] compressed, int compressedOffset, int compressedSize,
        byte[] uncompressed, int uncompressedOffset, int uncompressedSize,
        CompressionMethod method, FArchive? reader = null, EGame? game = null)
    {
        var src = new ReadOnlySpan<byte>(compressed, compressedOffset, compressedSize);
        var dst = new Span<byte>(uncompressed, uncompressedOffset, uncompressedSize);
        Decompress(src, dst, method, reader, game);
    }

    public static void Decompress(
        ReadOnlySpan<byte> compressed,
        Span<byte> uncompressed,
        CompressionMethod method, FArchive? reader = null, EGame? game = null)
    {
        CompressionAlgorithm algorithm = method switch
        {
            CompressionMethod.None => 0,
            CompressionMethod.Zlib or CompressionMethod.XB1Zlib or CompressionMethod.XboxOneGDKZlib => CompressionAlgorithm.Zlib,
            CompressionMethod.Gzip => CompressionAlgorithm.Gzip,
            CompressionMethod.Oodle => CompressionAlgorithm.Oodle,
            CompressionMethod.LZ4 => CompressionAlgorithm.LZ4,
            CompressionMethod.LZO => CompressionAlgorithm.LZO,
            CompressionMethod.Brotli => CompressionAlgorithm.Brotli,
            CompressionMethod.Zstd => CompressionAlgorithm.Zstd,
            _ when reader is not null => throw new UnknownCompressionMethodException(reader, $"Compression method \"{method}\" is unknown"),
            _ => throw new UnknownCompressionMethodException($"Compression method \"{method}\" is unknown")
        };

        if (algorithm == 0)
        {
            compressed.CopyTo(uncompressed);
            return;
        }

        // GAME_SleeplessWilds marks plain Leviathan blocks with the 0x8C 0x14 sub-code, which Oodle
        // builds predating the one shipped with the game refuse to decode. Rewriting it to the
        // standard 0x8C 0x0C is a byte-level fix-up, so the block is still decoded exactly once.
        var patched = algorithm == CompressionAlgorithm.Oodle && UsesNonStandardLeviathanSubCode(game ?? reader?.Game)
            ? OodleHelper.TryPatchNonStandardBlockHeader(compressed)
            : null;

        try
        {
            var source = patched is null ? compressed : patched.AsSpan(0, compressed.Length);

            if (!_decompressor.TryDecompress(algorithm, source, uncompressed, out int bytesWritten) || bytesWritten != uncompressed.Length)
            {
                throw new FileLoadException($"Failed to decompress {method} data (Expected: {uncompressed.Length}, Result: {bytesWritten})");
            }
        }
        finally
        {
            if (patched is not null) ArrayPool<byte>.Shared.Return(patched);
        }
    }

    /// <summary>
    /// Whether <paramref name="game"/> is known to write plain Leviathan blocks behind the
    /// non-standard 0x8C 0x14 sub-code, see <see cref="OodleHelper.TryPatchNonStandardBlockHeader"/>.
    /// </summary>
    private static bool UsesNonStandardLeviathanSubCode(EGame? game) => game is EGame.GAME_SleeplessWilds;
}
