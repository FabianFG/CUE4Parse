using System;
using System.Linq;
using System.Runtime.InteropServices;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Core.Serialization;
using CUE4Parse.UE4.Versions;
using static CUE4Parse.UE4.Objects.Core.Misc.ECompressionFlags;
using static CUE4Parse.UE4.Versions.EUnrealEngineObjectUE4Version;

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
        private const uint PACKAGE_FILE_TAG = 0x9E2A83C1U;
        private const uint PACKAGE_FILE_TAG_SWAPPED = 0xC1832A9EU;
        private const uint PACKAGE_FILE_TAG_ONE = 0x00656E6FU; // SOD2

        public readonly uint Tag;
        public readonly int LegacyFileVersion;
        public readonly LegacyUE3Version LegacyUE3Version;
        public readonly EUnrealEngineObjectUE4Version FileVersionUE4;
        public readonly EUnrealEngineObjectLicenseeUE4Version FileVersionLicenseUE4;
        public readonly FCustomVersion[] CustomVersionContainer;
        public int TotalHeaderSize;
        public readonly string FolderName;
        public EPackageFlags PackageFlags;
        public int NameCount;
        public readonly int NameOffset;
        public readonly string? LocalizationId;
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
        public readonly ECompressionFlags CompressionFlags;
        public readonly FCompressedChunk[] CompressedChunks;
        public readonly int PackageSource;
        public readonly string[] AdditionalPackagesToCook;
        public readonly int AssetRegistryDataOffset;
        public int BulkDataStartOffset; // serialized as long
        public readonly int WorldTileInfoDataOffset;
        public readonly int[] ChunkIds;
        public readonly int PreloadDependencyCount;
        public readonly int PreloadDependencyOffset;

        public readonly bool bUnversioned;

        public FPackageFileSummary()
        {
            CustomVersionContainer = Array.Empty<FCustomVersion>();
            FolderName = string.Empty;
            Generations = Array.Empty<FGenerationInfo>();
            CompressedChunks = Array.Empty<FCompressedChunk>();
            AdditionalPackagesToCook = Array.Empty<string>();
            ChunkIds = Array.Empty<int>();
        }

        internal FPackageFileSummary(FAssetArchive Ar)
        {
            Tag = Ar.Read<uint>();

            if (Tag == PACKAGE_FILE_TAG_ONE) // SOD2, "one"
            {
                Ar.Game = EGame.GAME_SOD2;
                Ar.Ver = Ar.Game.GetVersion();
                var legacyFileVersion = Ar.Read<int>(); // seems to be always int.MinValue
                bUnversioned = true;
                FileVersionUE4 = (EUnrealEngineObjectUE4Version) Ar.Ver;
                CustomVersionContainer = Array.Empty<FCustomVersion>();
                FolderName = "None";
                PackageFlags = EPackageFlags.PKG_FilterEditorOnly;
                goto afterPackageFlags;
            }

            if (Tag != PACKAGE_FILE_TAG && Tag != PACKAGE_FILE_TAG_SWAPPED)
            {
                throw new ParserException($"Invalid uasset magic: 0x{Tag:X8} != 0x{PACKAGE_FILE_TAG:X8}");
            }

            // The package has been stored in a separate endianness than the linker expected so we need to force
            // endian conversion. Latent handling allows the PC version to retrieve information about cooked packages.
            if (Tag == PACKAGE_FILE_TAG_SWAPPED)
            {
                // Set proper tag.
                //Tag = PACKAGE_FILE_TAG;
                // Toggle forced byte swapping.
                throw new ParserException("Byte swapping for packages not supported");
            }

            /*
            * The package file version number when this package was saved.
            *
            * Lower 16 bits stores the UE3 engine version
            * Upper 16 bits stores the UE4/licensee version
            * For newer packages this is -7
            *		-2 indicates presence of enum-based custom versions
            *		-3 indicates guid-based custom versions
            *		-4 indicates removal of the UE3 version. Packages saved with this ID cannot be loaded in older engine versions
            *		-5 indicates the replacement of writing out the "UE3 version" so older versions of engine can gracefully fail to open newer packages
            *		-6 indicates optimizations to how custom versions are being serialized
            *		-7 indicates the texture allocation info has been removed from the summary
            */
            const int CurrentLegacyFileVersion = -7;
            LegacyFileVersion = Ar.Read<int>();

            if (LegacyFileVersion < 0) // means we have modern version numbers
            {
                if (LegacyFileVersion < CurrentLegacyFileVersion)
                {
                    // we can't safely load more than this because the legacy version code differs in ways we can not predict.
                    // Make sure that the linker will fail to load with it.
                    throw new ParserException("Can't load legacy UE3 file");
                }

                LegacyUE3Version = LegacyFileVersion != -4 ? Ar.Read<LegacyUE3Version>() : 0;
                FileVersionUE4 = Ar.Read<EUnrealEngineObjectUE4Version>();
                FileVersionLicenseUE4 = Ar.Read<EUnrealEngineObjectLicenseeUE4Version>();
                CustomVersionContainer = LegacyFileVersion <= -2 ? Ar.ReadArray<FCustomVersion>() : Array.Empty<FCustomVersion>();

                if (Ar.Versions.CustomVersions == null && CustomVersionContainer.Length > 0)
                {
                    Ar.Versions.CustomVersions = CustomVersionContainer.ToList();
                }

                if (FileVersionUE4 == 0 && FileVersionLicenseUE4 == 0)
                {
                    // this file is unversioned, remember that, then use current versions
                    bUnversioned = true;
                    // set the versions to latest here, etc.
                    FileVersionUE4 = (EUnrealEngineObjectUE4Version) Ar.Ver;
                    FileVersionLicenseUE4 = EUnrealEngineObjectLicenseeUE4Version.VER_LIC_AUTOMATIC_VERSION;
                }
                else
                {
                    bUnversioned = false;
                    // Only apply the version if an explicit version is not set
                    if (!Ar.Versions.bExplicitVer)
                    {
                        Ar.Ver = (UE4Version) FileVersionUE4;
                    }
                }
            }
            else
            {
                // This is probably an old UE3 file, make sure that the linker will fail to load with it.
                throw new ParserException("Can't load legacy UE3 file");
            }

            TotalHeaderSize = Ar.Read<int>();
            FolderName = Ar.ReadFString();
            PackageFlags = Ar.Read<EPackageFlags>();

            afterPackageFlags:
            NameCount = Ar.Read<int>();
            NameOffset = Ar.Read<int>();

            LocalizationId = !PackageFlags.HasFlag(EPackageFlags.PKG_FilterEditorOnly) && FileVersionUE4 >= VER_UE4_ADDED_PACKAGE_SUMMARY_LOCALIZATION_ID ? Ar.ReadFString() : null;

            if (FileVersionUE4 >= VER_UE4_SERIALIZE_TEXT_IN_PACKAGES)
            {
                GatherableTextDataCount = Ar.Read<int>();
                GatherableTextDataOffset = Ar.Read<int>();
            }
            else
            {
                GatherableTextDataCount = GatherableTextDataOffset = 0;
            }

            ExportCount = Ar.Read<int>();
            ExportOffset = Ar.Read<int>();
            ImportCount = Ar.Read<int>();
            ImportOffset = Ar.Read<int>();
            DependsOffset = Ar.Read<int>();

            if (FileVersionUE4 is < VER_UE4_OLDEST_LOADABLE_PACKAGE or > VER_UE4_AUTOMATIC_VERSION)
            {
                SoftPackageReferencesCount = 0;
                SoftPackageReferencesOffset = 0;
                SearchableNamesOffset = 0;
                ThumbnailTableOffset = 0;
                Guid = default;
                Generations = Array.Empty<FGenerationInfo>();
                SavedByEngineVersion = default;
                CompatibleWithEngineVersion = default;
                CompressionFlags = COMPRESS_None;
                PackageSource = 0;
                AssetRegistryDataOffset = 0;
                WorldTileInfoDataOffset = 0;
                ChunkIds = Array.Empty<int>();
                AdditionalPackagesToCook = Array.Empty<string>();
                CompressedChunks = Array.Empty<FCompressedChunk>();
                PreloadDependencyCount = 0;
                PreloadDependencyOffset = 0;
                return; // we can't safely load more than this because the below was different in older files.
            }

            if (FileVersionUE4 >= VER_UE4_ADD_STRING_ASSET_REFERENCES_MAP)
            {
                SoftPackageReferencesCount = Ar.Read<int>();
                SoftPackageReferencesOffset = Ar.Read<int>();
            }
            else
            {
                SoftPackageReferencesCount = SoftPackageReferencesOffset = 0;
            }

            SearchableNamesOffset = FileVersionUE4 >= VER_UE4_ADDED_SEARCHABLE_NAMES ? Ar.Read<int>() : 0;
            ThumbnailTableOffset = Ar.Read<int>();

            if (Ar.Game == EGame.GAME_VALORANT) Ar.Position += 8;

            Guid = Ar.Read<FGuid>();

            Generations = Ar.ReadArray<FGenerationInfo>();

            if (FileVersionUE4 >= VER_UE4_ENGINE_VERSION_OBJECT)
            {
                SavedByEngineVersion = new FEngineVersion(Ar);

                if (FileVersionUE4 < VER_UE4_CORRECT_LICENSEE_FLAG
                    && SavedByEngineVersion.Major == 4
                    && SavedByEngineVersion.Minor == 26
                    && SavedByEngineVersion.Patch == 0
                    && SavedByEngineVersion.Changelist >= 12740027
                    && SavedByEngineVersion.IsLicenseeVersion())
                {
                    SavedByEngineVersion.Set(4, 26, 0, SavedByEngineVersion.Changelist, SavedByEngineVersion.Branch);
                }
            }
            else
            {
                var engineChangelist = Ar.Read<int>();
                SavedByEngineVersion = engineChangelist == 0 ? null : new FEngineVersion(4, 0, 0, (uint) engineChangelist, string.Empty);
            }

            CompatibleWithEngineVersion = FileVersionUE4 >= VER_UE4_PACKAGE_SUMMARY_HAS_COMPATIBLE_ENGINE_VERSION ? new FEngineVersion(Ar) : SavedByEngineVersion;

            static bool VerifyCompressionFlagsValid(int InCompressionFlags)
            {
                const int CompressionFlagsMask = (int) (COMPRESS_DeprecatedFormatFlagsMask | COMPRESS_OptionsFlagsMask);

                return (InCompressionFlags & ~CompressionFlagsMask) == 0;

                // @todo: check the individual flags here
            }

            CompressionFlags = Ar.Read<ECompressionFlags>();

            if (!VerifyCompressionFlagsValid((int) CompressionFlags))
            {
                throw new ParserException($"Incompatible compression flags ({(uint) CompressionFlags})");
            }

            CompressedChunks = Ar.ReadArray<FCompressedChunk>();

            if (CompressedChunks.Length != 0)
            {
                throw new ParserException("Package level compression is enabled");
            }

            PackageSource = Ar.Read<int>();
            AdditionalPackagesToCook = Ar.ReadArray(Ar.ReadFString);

            if (LegacyFileVersion > -7)
            {
                var NumTextureAllocations = Ar.Read<int>();
                if (NumTextureAllocations != 0)
                {
                    // We haven't used texture allocation info for ages and it's no longer supported anyway
                    throw new ParserException("Can't load legacy UE3 file");
                }
            }

            if (Ar.Ver >= UE4Version.VER_UE4_ASSET_REGISTRY_TAGS)
            {
                AssetRegistryDataOffset = Ar.Read<int>();
            }

            if (Ar.Game == EGame.GAME_SEAOFTHIEVES)
            {
                Ar.Position += 6; // no idea what's going on here.
            }

            if (Ar.Ver >= UE4Version.VER_UE4_SUMMARY_HAS_BULKDATA_OFFSET)
            {
                BulkDataStartOffset = (int) Ar.Read<long>();
            }

            WorldTileInfoDataOffset = FileVersionUE4 >= VER_UE4_WORLD_LEVEL_INFO ? Ar.Read<int>() : 0;

            switch (FileVersionUE4)
            {
                case >= VER_UE4_CHANGED_CHUNKID_TO_BE_AN_ARRAY_OF_CHUNKIDS:
                    ChunkIds = Ar.ReadArray<int>();
                    break;
                case >= VER_UE4_ADDED_CHUNKID_TO_ASSETDATA_AND_UPACKAGE:
                    var chunkId = Ar.Read<int>();
                    ChunkIds = chunkId < 0 ? Array.Empty<int>() : new[] { chunkId };
                    break;
                default:
                    ChunkIds = Array.Empty<int>();
                    break;
            }

            if (FileVersionUE4 >= VER_UE4_PRELOAD_DEPENDENCIES_IN_COOKED_EXPORTS)
            {
                PreloadDependencyCount = Ar.Read<int>();
                PreloadDependencyOffset = Ar.Read<int>();
            }
            else
            {
                PreloadDependencyCount = -1;
                PreloadDependencyOffset = 0;
            }

            if (Tag == PACKAGE_FILE_TAG_ONE)
            {
                Ar.AbsoluteOffset = NameOffset - (int) Ar.Position;
            }
        }
    }
}