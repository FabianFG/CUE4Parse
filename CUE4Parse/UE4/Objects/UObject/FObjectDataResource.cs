using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.UObject
{
    public enum EObjectDataResourceFlags : uint
    {
        None = 0,
        Inline = (1 << 0),
        Streaming = (1 << 1),
        Optional = (1 << 2),
        Duplicate = (1 << 3),
        MemoryMapped = (1 << 4),
        DerivedDataReference = (1 << 5),
    };

    public enum EObjectDataResourceVersion : uint
    {
        Invalid,
        Initial,
        LatestPlusOne,
        Latest = LatestPlusOne - 1
    };

    public readonly struct FObjectDataResource
    {
        public readonly EObjectDataResourceFlags Flags = EObjectDataResourceFlags.None;
        public readonly long SerialOffset = -1;
        public readonly long DuplicateSerialOffset = -1;
        public readonly long SerialSize = -1;
        public readonly long RawSize = -1;
        public readonly FPackageIndex OuterIndex;
        public readonly uint LegacyBulkDataFlags = 0;

        public FObjectDataResource(FAssetArchive Ar)
        {
            Flags = (EObjectDataResourceFlags) Ar.Read<uint>();
            SerialOffset = Ar.Read<long>();
            DuplicateSerialOffset = Ar.Read<long>();
            SerialSize = Ar.Read<long>();
            RawSize = Ar.Read<long>();
            OuterIndex = new FPackageIndex(Ar);
            LegacyBulkDataFlags = Ar.Read<uint>();
        }
    }
}
