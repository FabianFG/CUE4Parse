using System.IO;
using System.IO.Compression;

using CUE4Parse.UE4.Writers;

using CUE4Parse_Conversion.UEFormat.Enums;
using CUE4Parse_Conversion.UEFormat.Structs;

using ZstdSharp;

namespace CUE4Parse_Conversion.UEFormat;

public class UEFormatExport
{
    protected virtual string Identifier { get; set; }
    protected readonly FArchiveWriter Ar = new();
    protected ExporterOptions Options;
    private string ObjectName;
    
    private const int ZSTD_LEVEL = 6; // let user change eventually?
    
    protected UEFormatExport(string name, ExporterOptions options)
    {
        ObjectName = name;
        Options = options;
    }

    public void Save(FArchiveWriter archive)
    {
        var header = new FUEFormatHeader(Identifier, ObjectName, Options.CompressionFormat);
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
