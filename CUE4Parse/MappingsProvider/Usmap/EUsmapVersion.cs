namespace CUE4Parse.MappingsProvider.Usmap
{
    public enum EUsmapVersion : byte
    {
        /* Initial format. */
        Initial,

        /* Adds package versioning to aid with compatibility */
        PackageVersioning,

        /* Adds support for 16-bit wide name-lengths (ushort/uint16) */
        LongFName,

        /* Adds support for enums with more than 255 values */
        LargeEnums,

        LatestPlusOne,
        Latest = LatestPlusOne - 1
    }
}