using System;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Objects.UObject
{
    /// <summary>
    /// Revision data for an Unreal package file.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct FGenerationInfo
    {
        /**
         * Number of exports in the linker's ExportMap for this generation.
         */
        public readonly int ExportCount; 
        
        /**
         * Number of names in the linker's NameMap for this generation.
         */
        public readonly int NameCount;
    }
    
    public class FPackageFileSummary
    {
        public readonly uint Tag;
        public readonly int LegacyFileVersion;
        public readonly int LegacyUE3Version;
        public readonly int FileVersionUE4;
        public readonly int FileVersionLicenseUE4;
        public readonly FCustomVersion[] CustomContainerVersion;
        public int TotalHeaderSize;
        public readonly string FolderName;
        public PackageFlags PackageFlags;
        public int NameCount;
        public readonly int NameOffset;
        public readonly int GatherableTextDataCount;
        public readonly int GatherableTextDataOffset;
        public int ExportCount;
        public readonly int ExportOffset;
        public int ImportCount;
        public readonly int ImportOffset;
        public readonly int DependsOffset;
        public readonly int SoftPackageReferencesCount;
        public readonly int SoftPackageReferencesOffset;
        public readonly int SearchableNamesOffset;
        public readonly int ThumbnailTableOffset;
        public readonly FGuid Guid;
        public readonly FGenerationInfo[] Generations;
        public readonly FEngineVersion? SavedByEngineVersion;
        public readonly FEngineVersion? CompatibleWithEngineVersion;
        public readonly uint CompressionFlags;
        public readonly FCompressedChunk[] CompressedChunks;
        public readonly uint PackageSource;
        public readonly string[] AdditionalPackagesToCook;
        public readonly int AssetRegistryDataOffset;
        public int BulkDataStartOffset;
        public readonly int WorldTileInfoDataOffset;
        public readonly int[] ChunkIds;
        public readonly int PreloadDependencyCount;
        public readonly int PreloadDependencyOffset;

        public FPackageFileSummary()
        {
            CustomContainerVersion = Array.Empty<FCustomVersion>();
            FolderName = string.Empty;
            Generations = Array.Empty<FGenerationInfo>();
            CompressedChunks = Array.Empty<FCompressedChunk>();
            AdditionalPackagesToCook = Array.Empty<string>();
            ChunkIds = Array.Empty<int>();
        }

        public FPackageFileSummary(FArchive Ar)
        {
            Tag = Ar.Read<uint>();
            LegacyFileVersion = Ar.Read<int>();
            LegacyUE3Version = Ar.Read<int>();
            FileVersionUE4 = Ar.Read<int>();
            FileVersionLicenseUE4 = Ar.Read<int>();
            CustomContainerVersion = Ar.ReadArray<FCustomVersion>();
            TotalHeaderSize = Ar.Read<int>();
            FolderName = Ar.ReadFString();
            PackageFlags = Ar.Read<PackageFlags>();
            NameCount = Ar.Read<int>();
            NameOffset = Ar.Read<int>();
            GatherableTextDataCount = Ar.Read<int>();
            GatherableTextDataOffset = Ar.Read<int>();
            ExportCount = Ar.Read<int>();
            ExportOffset = Ar.Read<int>();
            ImportCount = Ar.Read<int>();
            ImportOffset = Ar.Read<int>();
            DependsOffset = Ar.Read<int>();
            SoftPackageReferencesCount = Ar.Read<int>();
            SoftPackageReferencesOffset = Ar.Read<int>();
            SearchableNamesOffset = Ar.Read<int>();
            ThumbnailTableOffset = Ar.Read<int>();
            Guid = Ar.Read<FGuid>();
            Generations = Ar.ReadArray<FGenerationInfo>();
            SavedByEngineVersion = new FEngineVersion(Ar);
            CompatibleWithEngineVersion = new FEngineVersion(Ar);
            CompressionFlags = Ar.Read<uint>();
            CompressedChunks = Ar.ReadArray<FCompressedChunk>();
            PackageSource = Ar.Read<uint>();
            AdditionalPackagesToCook = Ar.ReadArray(Ar.ReadFString);
            AssetRegistryDataOffset = Ar.Read<int>();
            BulkDataStartOffset = Ar.Read<int>();
            WorldTileInfoDataOffset = Ar.Read<int>();
            ChunkIds = Ar.ReadArray<int>();
            PreloadDependencyCount = Ar.Read<int>();
            PreloadDependencyOffset = Ar.Read<int>();
        }
    }
}