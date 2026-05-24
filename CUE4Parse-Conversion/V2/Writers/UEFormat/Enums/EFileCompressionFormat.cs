using System.ComponentModel;

namespace CUE4Parse_Conversion.V2.Writers.UEFormat.Enums;

public enum EFileCompressionFormat
{
    [Description("Uncompressed")]
    None,

    [Description("Gzip Compression")]
    GZIP,

    [Description("ZStandard Compression")]
    ZSTD
}
