namespace CUE4Parse.MappingsProvider.Usmap;

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

    /* Adds support for explicit enum values */
    ExplicitEnumValues,

    /* Adds support for engine versioning information */
    EngineVersioning,

    /* Property Flags, PackageOwnerName, Usmap Metadata, Class/Struct flags, extends ArrayDim to ushort/uint16
     and PropertyCount to int (actual count is int24 and 1 byte flag for Class/Struct flag) */
    ExtendedMetadata,

    LatestPlusOne,
    Latest = LatestPlusOne - 1
}