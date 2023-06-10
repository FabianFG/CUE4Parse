using System.Runtime.InteropServices;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.IO.Objects
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FIoContainerHeaderLocalizedPackage
    {
        public readonly FPackageId SourcePackageId;
        public readonly FMappedName SourcePackageName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FIoContainerHeaderPackageRedirect
    {
        public readonly FPackageId SourcePackageId;
        public readonly FPackageId TargetPackageId;
        public readonly FMappedName SourcePackageName;
    }

    public enum EIoContainerHeaderVersion // : uint
    {
        BeforeVersionWasAdded = -1, // Custom constant to indicate pre-UE5 data
        Initial = 0,
        LocalizedPackages = 1,
        OptionalSegmentPackages = 2,
        NoExportInfo = 3,

        LatestPlusOne,
        Latest = LatestPlusOne - 1
    }

    public class FIoContainerHeader
    {
        private const int Signature = 0x496f436e;
        public FIoContainerId ContainerId;

        public FPackageId[] PackageIds;
        public FFilePackageStoreEntry[] StoreEntries;
        public FFilePackageStoreEntry[] OptionalSegmentStoreEntries;
        public FPackageId[] OptionalSegmentPackageIds;

        public FNameEntrySerialized[]? ContainerNameMap; // RedirectsNameMap
        // public FIoContainerHeaderLocalizedPackage[]? LocalizedPackages;
        // public FIoContainerHeaderPackageRedirect[] PackageRedirects;

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

            ReadPackageIdsAndEntries(Ar, out PackageIds, out StoreEntries, version);

            if (version >= EIoContainerHeaderVersion.OptionalSegmentPackages)
            {
                ReadPackageIdsAndEntries(Ar, out OptionalSegmentPackageIds, out OptionalSegmentStoreEntries, version);
            }
            if (version >= EIoContainerHeaderVersion.Initial)
            {
                ContainerNameMap = FNameEntrySerialized.LoadNameBatch(Ar);
            }
            // if (version >= EIoContainerHeaderVersion.LocalizedPackages)
            // {
            //     LocalizedPackages = Ar.ReadArray<FIoContainerHeaderLocalizedPackage>();
            // }
            // PackageRedirects = Ar.ReadArray<FIoContainerHeaderPackageRedirect>();
        }

        private void ReadPackageIdsAndEntries(FArchive Ar, out FPackageId[] packageIds, out FFilePackageStoreEntry[] storeEntries, EIoContainerHeaderVersion version)
        {
            packageIds = Ar.ReadArray<FPackageId>();
            var storeEntriesSize = Ar.Read<int>();
            var storeEntriesEnd = Ar.Position + storeEntriesSize;
            storeEntries = Ar.ReadArray(packageIds.Length, () => new FFilePackageStoreEntry(Ar, version));
            Ar.Position = storeEntriesEnd;
        }
    }
}
