using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;

using K4os.Compression.LZ4;

using OffiUtils;

using OodleDotNet;

using OodleSharp;

using ZlibngDotNet;

using ZstdSharpMethods = ZstdSharp.Unsafe.Methods;

namespace CUE4Parse.Compression;

public static class Compression
{
    public const int LOADING_COMPRESSION_CHUNK_SIZE = 131072;

    public static IDecompressor Decompressor => _decompressor;

    private static unsafe IDecompressor _decompressor = DecompressorBuilder.Default
        .Add(CompressionAlgorithm.Oodle, OodleDecompressor.TryDecompress)
        .Add(CompressionAlgorithm.LZ4, static (source, destination, out written)
            => (written = LZ4Codec.Decode(source, destination)) > 0, replace: true)
        .Add(CompressionAlgorithm.Zstd, static (source, destination, out written) =>
        {
            fixed (byte* srcPtr = source)
            fixed (byte* dstPtr = destination)
            {
                var result = ZstdSharpMethods.ZSTD_decompress(
                    dstPtr, (nuint) destination.Length, srcPtr, (nuint) source.Length);
                if (ZstdSharpMethods.ZSTD_isError(result))
                {
                    written = 0;
                    return false;
                }

                written = (int) result;
                return true;
            }
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
        CompressionAlgorithm algorythm = method switch
        {
            CompressionMethod.None => 0,
            CompressionMethod.Zlib or CompressionMethod.XB1Zlib or CompressionMethod.XboxOneGDKZlib => CompressionAlgorithm.Zlib,
            CompressionMethod.Gzip => CompressionAlgorithm.Gzip,
            CompressionMethod.Oodle => CompressionAlgorithm.Oodle,
            CompressionMethod.LZ4 => CompressionAlgorithm.LZ4,
            CompressionMethod.Brotli => CompressionAlgorithm.Brotli,
            CompressionMethod.Zstd => CompressionAlgorithm.Zstd,
            _ when reader is not null => throw new UnknownCompressionMethodException(reader, $"Compression method \"{method}\" is unknown"),
            _ => throw new UnknownCompressionMethodException($"Compression method \"{method}\" is unknown")
        };

        if (algorythm == 0)
        {
            compressed.CopyTo(uncompressed);
            return;
        }

        if (!_decompressor.TryDecompress(algorythm, compressed, uncompressed, out int bytesWritten) || bytesWritten != uncompressed.Length)
        {
            throw new FileLoadException($"Failed to decompress {method} data (Expected: {uncompressed.Length}, Result: {bytesWritten})");
        }
    }
}
