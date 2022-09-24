namespace CUE4Parse.MappingsProvider.Usmap
{
    public enum EUsmapVersion : byte
    {
        /** Initial format. */
        Initial,

        /** Adds package versioning to aid with compatibility */
        PackageVersioning,

        LatestPlusOne,
        Latest = LatestPlusOne - 1
    }
}