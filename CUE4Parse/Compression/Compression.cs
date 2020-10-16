using System;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.Compression
{
    public static class Compression
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Decompress(byte[] src, int uncompressedSize, CompressionMethod method, FArchive? reader = null)
        {
            var dst = new byte[uncompressedSize];
            Decompress(src, dst, method);
            return dst;
        }
        public static void Decompress(byte[] src, byte[] dst, CompressionMethod method, FArchive? reader = null)
        {
            switch (method)
            {
                case CompressionMethod.None:
                    break;
                case CompressionMethod.Zlib:
                    break;
                case CompressionMethod.Gzip:
                    break;
                case CompressionMethod.Oodle:
                    break;
                case CompressionMethod.Unknown:
                    break;
                default:
                    if (reader != null) throw new UnknownCompressionMethodException(reader, $"Compression method \"{method}\" is unknown");
                    else throw new UnknownCompressionMethodException($"Compression method \"{method}\" is unknown");
            }
        }
    }
}