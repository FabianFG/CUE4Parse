using System;
using System.Text;
using CUE4Parse_Conversion.UEFormat.Enums;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.UEFormat.Structs;

public struct FUEFormatHeader : ISerializable
{
    public EFileCompressionFormat CompressionFormat;
    public int CompressedSize;
    public int UncompressedSize;
    
    private readonly string Identifier;
    private EUEFormatVersion FileVersion;
    private string ObjectName;
    private const string MAGIC = "UEFORMAT";

    public FUEFormatHeader(string identifier, string objectName, EFileCompressionFormat compressionFormat = EFileCompressionFormat.None)
    {
        Identifier = identifier;
        ObjectName = objectName;
        CompressionFormat = compressionFormat;
        FileVersion = EUEFormatVersion.LatestVersion;
    }
    
    public void Serialize(FArchiveWriter Ar)
    {
        var padded = new byte[MAGIC.Length];
        var bytes = Encoding.UTF8.GetBytes(MAGIC); 
        Buffer.BlockCopy(bytes, 0, padded, 0, bytes.Length);
        Ar.Write(padded);
        
        Ar.WriteFString(Identifier);
        Ar.Write((byte) FileVersion);
        Ar.WriteFString(ObjectName);

        var isCompressed = CompressionFormat != EFileCompressionFormat.None;
        Ar.Write(isCompressed);
        if (isCompressed)
        {
            Ar.WriteFString(CompressionFormat.ToString());
            Ar.Write(UncompressedSize);
            Ar.Write(CompressedSize);
        }
    }
}