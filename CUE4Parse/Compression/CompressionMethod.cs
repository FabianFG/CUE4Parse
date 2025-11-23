using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.Compression;

[JsonConverter(typeof(StringEnumConverter))]
public enum CompressionMethod
{
    None = 0,
    Zlib = 1,
    Gzip = 2, //???
    Custom = 3,
    Oodle = 4,
    LZ4,
    LZO,
    Zstd,
    XB1Zlib,
    XboxOneGDKZlib,
    Unknown
}
