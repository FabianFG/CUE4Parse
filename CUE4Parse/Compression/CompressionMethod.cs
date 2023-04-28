namespace CUE4Parse.Compression
{
    public enum CompressionMethod
    {
        None = 0,
        Zlib = 1,
        Gzip = 2, //???
        Custom = 3,
        Oodle = 4,
        LZ4,
        Zstd,
        Unknown
    }
}
