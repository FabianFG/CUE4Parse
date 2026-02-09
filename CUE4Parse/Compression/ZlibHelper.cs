using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;

using Serilog;

using ZlibngDotNet;

namespace CUE4Parse.Compression;

public class ZlibException : ParserException
{
    public ZlibException(string? message = null, Exception? innerException = null) : base(message, innerException) { }
    public ZlibException(FArchive reader, string? message = null, Exception? innerException = null) : base(reader, message, innerException) { }
}

public static class ZlibHelper
{
    [Obsolete]
    public static void Initialize(string path)
    {
    }

    [Obsolete]
    public static void Initialize(Zlibng instance)
    {
    }

    [Obsolete]
    public static bool DownloadDll(string? path = null, string? url = null)
    {
        return true;
    }

    public static void Decompress(byte[] compressed, int compressedOffset, int compressedSize,
        byte[] uncompressed, int uncompressedOffset, int uncompressedSize, FArchive? reader = null)
    {
        using MemoryStream memoryStream = new MemoryStream(compressed, compressedOffset, compressedSize);
        using ZLibStream zlibStream = new ZLibStream(memoryStream, CompressionMode.Decompress);

        try
        {
            int decodedSize = zlibStream.Read(uncompressed.AsSpan(uncompressedOffset, uncompressedSize));
            if (decodedSize < uncompressedSize)
            {
                // Not sure whether this should be an exception or not
                Log.Warning("Zlib decompression only decompressed {0} bytes of the expected {1} bytes", decodedSize, uncompressedSize);
            }
        }
        catch (Exception ex)
        {
            throw new ZlibException(message: ex.Message, innerException: ex);
        }
    }

    [Obsolete]
    public static async Task<bool> DownloadDllAsync(string? path, string? url = null)
    {
        return true;
    }
}
