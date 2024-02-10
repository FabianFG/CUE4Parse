namespace CUE4Parse.MappingsProvider.Usmap
{
    public enum EUsmapVersion : byte
    {
        /** Initial format. */
        Initial,

        /** Adds package versioning to aid with compatibility */
        PackageVersioning,

        /** Increases size of names in lookup table to ushort from byte */
        LongFName,

        LatestPlusOne,
        Latest = LatestPlusOne - 1
    }
}