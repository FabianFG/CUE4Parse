using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;

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

    private static volatile bool _nonStandardOodleHeaderLogged;

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
        CompressionMethod method, FArchive? reader = null)
    {
        var src = new ReadOnlySpan<byte>(compressed, compressedOffset, compressedSize);
        var dst = new Span<byte>(uncompressed, uncompressedOffset, uncompressedSize);
        Decompress(src, dst, method, reader);
    }

    public static void Decompress(
        ReadOnlySpan<byte> compressed,
        Span<byte> uncompressed,
        CompressionMethod method, FArchive? reader = null)
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

        if (TryDecompress(algorithm, compressed, uncompressed, out int bytesWritten))
        {
            return;
        }

        // Some UE 5.5 builds write a Leviathan block header with a sub-code older Oodle versions
        // refuse (0x8C 0x14 instead of 0x8C 0x0C) even though the payload is plain Leviathan.
        // Retry the very same payload with the standard sub-code before giving up.
        if (algorithm == CompressionAlgorithm.Oodle && TryDecompressPatchedOodle(compressed, uncompressed, out bytesWritten))
        {
            return;
        }

        throw new FileLoadException($"Failed to decompress {method} data (Expected: {uncompressed.Length}, Result: {bytesWritten})");
    }

    private static bool TryDecompress(
        CompressionAlgorithm algorithm,
        ReadOnlySpan<byte> compressed,
        Span<byte> uncompressed,
        out int bytesWritten) =>
        _decompressor.TryDecompress(algorithm, compressed, uncompressed, out bytesWritten) && bytesWritten == uncompressed.Length;

    private static bool TryDecompressPatchedOodle(
        ReadOnlySpan<byte> compressed,
        Span<byte> uncompressed,
        out int bytesWritten)
    {
        bytesWritten = 0;

        if (OodleHelper.TryPatchNonStandardBlockHeader(compressed) is not { } patched) return false;
        try
        {
            if (!TryDecompress(CompressionAlgorithm.Oodle, patched.AsSpan(0, compressed.Length), uncompressed, out bytesWritten))
            {
                return false;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(patched);
        }

        if (!_nonStandardOodleHeaderLogged)
        {
            _nonStandardOodleHeaderLogged = true;
            Log.Debug("Oodle block with a non-standard header (0x8C 0x14) decompressed using the standard Leviathan sub-code (0x8C 0x0C) instead");
        }

        return true;
    }
}
