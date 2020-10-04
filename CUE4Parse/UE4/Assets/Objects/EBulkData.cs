using System.Runtime.CompilerServices;

namespace CUE4Parse.UE4.Assets.Objects
{
    public enum EBulkData
    {
        BULKDATA_PayloadAtEndOfFile = 0x0001,               // bulk data stored at the end of this file, data offset added to global data offset in package
        BULKDATA_CompressedZlib = 0x0002,                   // the same value as for UE3
        BULKDATA_Unused = 0x0020,                           // the same value as for UE3
        BULKDATA_ForceInlinePayload = 0x0040,               // bulk data stored immediately after header
        BULKDATA_PayloadInSeperateFile = 0x0100,            // data stored in .ubulk file near the asset (UE4.12+)
        BULKDATA_SerializeCompressedBitWindow = 0x0200,     // use platform-specific compression
        BULKDATA_OptionalPayload = 0x0800,                  // same as BULKDATA_PayloadInSeperateFile, but stored with .uptnl extension (UE4.20+)
        BULKDATA_Size64Bit = 0x2000,                        // 64-bit size fields, UE4.22+
        BULKDATA_NoOffsetFixUp = 0x10000                    // do not add Summary.BulkDataStartOffset to bulk location, UE4.26
    }

    public static class BulkDataFlagUtil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Check(this EBulkData bulkData, int bulkDataFlags) => ((int) bulkData & bulkDataFlags) != 0;
    }
}