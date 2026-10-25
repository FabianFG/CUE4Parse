using System;
using System.IO;
using System.IO.Compression;
using CUE4Parse.UE4.Writers;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Writers.UEFormat.Enums;
using CUE4Parse_Conversion.Writers.UEFormat.Structs;
using ZstdSharp;

namespace CUE4Parse_Conversion.Writers.UEFormat;

public abstract class UEFormatExport(string objectName, string objectPath, ExportOptions options)
{
    protected abstract string Identifier { get; }

    protected readonly FArchiveWriter Ar = new();
    protected readonly EFileCompressionFormat CompressionFormat = options.CompressionFormat;
    private readonly string _objectName = objectName;
    private readonly string _objectPath = objectPath;

    private const int ZSTD_LEVEL = 6;

    protected void WriteRoot(Action<FDataAttributeSet> build)
    {
        var root = new FDataAttributeSet();
        build(root);
        root.Serialize(Ar);
    }

    public void Save(FArchiveWriter archive)
    {
        var header = new FUEFormatHeader(Identifier, _objectName, _objectPath, CompressionFormat);
        var data = Ar.GetBuffer();
        header.UncompressedSize = data.Length;

        var compressedData = header.CompressionFormat switch
        {
            EFileCompressionFormat.GZIP => GzipCompress(data),
            EFileCompressionFormat.ZSTD => new Compressor(ZSTD_LEVEL).Wrap(data),
            _ => data
        };
        header.CompressedSize = compressedData.Length;

        header.Serialize(archive);
        archive.Write(compressedData);
    }

    private static byte[] GzipCompress(byte[] src)
    {
        using var outStream = new MemoryStream();
        using var srcStream = new MemoryStream(src);
        using (var gzipStream = new GZipStream(outStream, CompressionMode.Compress))
        {
            srcStream.CopyTo(gzipStream);
        }

        return outStream.ToArray();
    }
}
