using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.IO.Objects
{
    public enum EZenPackageVersion : uint
    {
        Initial,

        LatestPlusOne,
        Latest = LatestPlusOne - 1
    }

    public struct FZenPackageVersioningInfo
    {
        public EZenPackageVersion ZenVersion;
        public FPackageFileVersion PackageVersion;
        public int LicenseeVersion;
        public FCustomVersion[] CustomVersions;

        public FZenPackageVersioningInfo(FArchive Ar)
        {
            ZenVersion = Ar.Read<EZenPackageVersion>();
            PackageVersion = Ar.Read<FPackageFileVersion>();
            LicenseeVersion = Ar.Read<int>();
            CustomVersions = Ar.ReadArray<FCustomVersion>();
        }
    }

    public readonly struct FZenPackageSummary
    {
        public readonly uint bHasVersioningInfo;
        public readonly uint HeaderSize;
        public readonly FMappedName Name;
        // public readonly FMappedName SourceName; // Removed after CL 17014898 of ue5-main
        public readonly EPackageFlags PackageFlags;
        public readonly uint CookedHeaderSize;
        public readonly int ImportMapOffset;
        public readonly int ExportMapOffset;
        public readonly int ExportBundleEntriesOffset;
        public readonly int GraphDataOffset;
    }
}