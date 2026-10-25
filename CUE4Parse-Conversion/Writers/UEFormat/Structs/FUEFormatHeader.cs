using System.Text;
using CUE4Parse_Conversion.Writers.UEFormat.Enums;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Writers.UEFormat.Structs;

public struct FUEFormatHeader : ISerializable
{
    public EFileCompressionFormat CompressionFormat;
    public int CompressedSize;
    public int UncompressedSize;

    private readonly string Identifier;
    private EUEFormatVersion FileVersion;
    private string ObjectName;
    private string ObjectPath;
    private const string MAGIC = "UEFORMAT";

    public FUEFormatHeader(string identifier, string objectName, string objectPath, EFileCompressionFormat compressionFormat = EFileCompressionFormat.None)
    {
        Identifier = identifier;
        ObjectName = objectName;
        ObjectPath = objectPath;
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
        Ar.WriteFString(ObjectPath);

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
