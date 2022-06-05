using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.IO.Objects
{
    public enum EIoContainerHeaderVersion // : uint
    {
        BeforeVersionWasAdded = -1, // Custom constant to indicate pre-UE5 data
        Initial = 0,
        LocalizedPackages = 1,
        OptionalSegmentPackages = 2,

        LatestPlusOne,
        Latest = LatestPlusOne - 1
    }

    public class FIoContainerHeader
    {
        private const int Signature = 0x496f436e;
        public FIoContainerId ContainerId;
        public FNameEntrySerialized[]? ContainerNameMap;
        public FPackageId[] PackageIds;
        public FFilePackageStoreEntry[] StoreEntries;
        public FPackageId[] OptionalSegmentPackageIds;
        public uint[] OptionalSegmentStoreEntries;

        public FIoContainerHeader(FArchive Ar)
        {
            var version = Ar.Game >= EGame.GAME_UE5_0 ? EIoContainerHeaderVersion.Initial : EIoContainerHeaderVersion.BeforeVersionWasAdded;
            if (version == EIoContainerHeaderVersion.Initial)
            {
                var signature = Ar.Read<uint>();
                if (signature != Signature)
                {
                    throw new ParserException(Ar, $"Invalid container header signature: 0x{signature:X8} != 0x{Signature:X8}");
                }

                version = Ar.Read<EIoContainerHeaderVersion>();
            }

            ContainerId = Ar.Read<FIoContainerId>();
            var packageCount = version < EIoContainerHeaderVersion.OptionalSegmentPackages ? Ar.Read<uint>() : 0;
            if (version == EIoContainerHeaderVersion.BeforeVersionWasAdded)
            {
                var namesSize = Ar.Read<int>();
                var namesPos = Ar.Position;
                var nameHashesSize = Ar.Read<int>();
                var continuePos = Ar.Position + nameHashesSize;
                Ar.Position = namesPos;
                ContainerNameMap = FNameEntrySerialized.LoadNameBatch(Ar, nameHashesSize / sizeof(ulong) - 1);
                Ar.Position = continuePos;
            }

            PackageIds = Ar.ReadArray<FPackageId>(); // < OptionalSegmentPackages ? Length = PackageCount (FN 20.0)
            var storeEntriesSize = Ar.Read<int>();
            var storeEntriesEnd = Ar.Position + storeEntriesSize;
            StoreEntries = Ar.ReadArray(PackageIds.Length, () => new FFilePackageStoreEntry(Ar));
            Ar.Position = storeEntriesEnd;

            if (version >= EIoContainerHeaderVersion.OptionalSegmentPackages)
            {
                OptionalSegmentPackageIds = Ar.ReadArray<FPackageId>();
                var optionalSegmentStoreEntriesSize = Ar.Read<int>();
                var optionalSegmentStoreEntriesEnd = Ar.Position + optionalSegmentStoreEntriesSize;
                OptionalSegmentStoreEntries = Ar.ReadArray<uint>(OptionalSegmentPackageIds.Length);
                Ar.Position = optionalSegmentStoreEntriesEnd;
            }
            if (version >= EIoContainerHeaderVersion.Initial)
            {
                ContainerNameMap = FNameEntrySerialized.LoadNameBatch(Ar); // Actual name is RedirectsNameMap
            }

            // Skip CulturePackageMap and PackageRedirects
        }
    }
}
