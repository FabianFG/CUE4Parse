namespace CUE4Parse.MappingsProvider.Usmap;

public enum EUsmapVersion : byte
{
    /** Initial format. */
    Initial,

    /** Changes EPropertyType to the official EClassCastFlags enum **/
    PropertyTypeToClassFlags,

    /** Adds package versioning to aid with compatibility */
    PackageVersioning,

    LatestPlusOne,
    Latest = LatestPlusOne - 1
}