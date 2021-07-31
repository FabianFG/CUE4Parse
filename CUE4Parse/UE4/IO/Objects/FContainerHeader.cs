using System;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.IO.Objects
{
    public class FContainerHeader
    {
        public FIoContainerId ContainerId;
        public uint PackageCount;
        public FNameEntrySerialized[] ContainerNameMap;
        public FPackageId[] PackageIds;
        public FPackageStoreEntry[] StoreEntries;

        public FContainerHeader(FArchive Ar)
        {
            ContainerId = Ar.Read<FIoContainerId>();
            PackageCount = Ar.Read<uint>();
            if (Ar.Game < EGame.GAME_UE5_0)
            {
                var namesSize = Ar.Read<int>();
                var namesPos = Ar.Position;
                var nameHashesSize = Ar.Read<int>();
                var continuePos = Ar.Position + nameHashesSize;
                Ar.Position = namesPos;
                ContainerNameMap = FNameEntrySerialized.LoadNameBatch(Ar, nameHashesSize / sizeof(ulong) - 1);
                Ar.Position = continuePos;
            }
            else
            {
                ContainerNameMap = Array.Empty<FNameEntrySerialized>();
            }

            PackageIds = Ar.ReadArray<FPackageId>();
            var storeEntriesSize = Ar.Read<int>();
            var storeEntriesEnd = Ar.Position + storeEntriesSize;
            StoreEntries = Ar.ReadArray((int) PackageCount, () => new FPackageStoreEntry(Ar));
            Ar.Position = storeEntriesEnd;
            // Skip CulturePackageMap and PackageRedirects
        }
    }
}