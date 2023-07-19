namespace CUE4Parse.UE4.Objects.Core.i18N
{
    public enum ELocResVersion : byte
    {
        /** Legacy format file - will be missing the magic number. */
        Legacy = 0,
        /** Compact format file - strings are stored in a LUT to avoid duplication. */
        Compact,
        /** Optimized format file - namespaces/keys are pre-hashed (CRC32), we know the number of elements up-front, and the number of references for each string in the LUT (to allow stealing). */
        Optimized_CRC32,
        /** Optimized format file - namespaces/keys are pre-hashed (CityHash64, UTF-16), we know the number of elements up-front, and the number of references for each string in the LUT (to allow stealing). */
        Optimized_CityHash64_UTF16,

        LatestPlusOne,
        Latest = LatestPlusOne - 1
    }
}