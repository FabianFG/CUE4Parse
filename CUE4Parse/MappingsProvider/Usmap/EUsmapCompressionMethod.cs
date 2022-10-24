namespace CUE4Parse.MappingsProvider.Usmap
{
    public enum EUsmapCompressionMethod : byte
    {
        None,
        Oodle,
        Brotli,
        ZStandard,

        Unknown = 0xFF
    }
}