using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using Ionic.Zlib;
using K4os.Compression.LZ4;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace CUE4Parse.Compression
{
    public static class Compression
    {
        public static byte[] Decompress(byte[] compressed, int uncompressedSize, CompressionMethod method, FArchive? reader = null) =>
            Decompress(compressed, 0, compressed.Length, uncompressedSize, method, reader);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Decompress(byte[] compressed, int compressedOffset, int compressedCount, int uncompressedSize, CompressionMethod method, FArchive? reader = null)
        {
            var uncompressed = new byte[uncompressedSize];
            Decompress(compressed, compressedOffset, compressedCount, uncompressed, 0, uncompressedSize, method);
            return uncompressed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Decompress(byte[] compressed, byte[] dst, CompressionMethod method, FArchive? reader = null) =>
            Decompress(compressed, 0, compressed.Length, dst, 0, dst.Length, method, reader);
        public static void Decompress(byte[] compressed, int compressedOffset, int compressedSize, byte[] uncompressed, int uncompressedOffset, int uncompressedSize, CompressionMethod method, FArchive? reader = null)
        {
            using var srcStream = new MemoryStream(compressed, compressedOffset, compressedSize, false) {Position = 0};
            switch (method)
            {
                case CompressionMethod.None:
                    Buffer.BlockCopy(compressed, compressedOffset, uncompressed, uncompressedOffset, compressedSize);
                    return;
                case CompressionMethod.Zlib:
                    var zlib = new ZlibStream(srcStream, CompressionMode.Decompress);
                    zlib.Read(uncompressed, uncompressedOffset, uncompressedSize);
                    zlib.Dispose();
                    return;
                case CompressionMethod.Gzip:
                    var gzip = new GZipStream(srcStream, CompressionMode.Decompress);
                    gzip.Read(uncompressed, uncompressedOffset, uncompressedSize);
                    gzip.Dispose();
                    return;
                case CompressionMethod.Oodle:
                    Oodle.Decompress(compressed, compressedOffset, compressedSize, uncompressed, uncompressedOffset, uncompressedSize, reader);
                    return;
                case CompressionMethod.LZ4:
                    var result = LZ4Codec.Decode(compressed, compressedOffset, compressedSize, uncompressed, uncompressedOffset, uncompressedSize);
                    if (result != uncompressedSize) throw new FileLoadException($"Failed to decompress data (Expected: {uncompressedSize}, Result: {result})");
                    //var lz4 = LZ4Stream.Decode(srcStream);
                    //lz4.Read(uncompressed, uncompressedOffset, uncompressedSize);
                    //lz4.Dispose();
                    return;
                default:
                    if (reader != null) throw new UnknownCompressionMethodException(reader, $"Compression method \"{method}\" is unknown");
                    else throw new UnknownCompressionMethodException($"Compression method \"{method}\" is unknown");
            }
        }
    }
}