using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.GameTypes.Rennsport.Encryption.Aes;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using GenericReader;
using OffiUtils;
using static CUE4Parse.Compression.Compression;
using static CUE4Parse.UE4.Pak.Objects.EPakFileVersion;

namespace CUE4Parse.UE4.Pak
{
    public partial class PakFileReader : AbstractAesVfsReader
    {
        public readonly FArchive Ar;
        public readonly FPakInfo Info;

        public override string MountPoint { get; protected set; }
        public sealed override long Length { get; set; }

        public override bool HasDirectoryIndex => true;
        public override FGuid EncryptionKeyGuid => Info.EncryptionKeyGuid;
        public override bool IsEncrypted => Info.EncryptedIndex;

        public PakFileReader(FArchive Ar) : base(Ar.Name, Ar.Versions)
        {
            this.Ar = Ar;
            Length = Ar.Length;
            Info = FPakInfo.ReadFPakInfo(Ar);
            CompressionMethods = Info.CompressionMethods.ToArray();

            var hasUnsupportedVersion = (Ar.Game < EGame.GAME_UE5_7 && Info.Version > PakFile_Version_Fnv64BugFix)
                || (Ar.Game >= EGame.GAME_UE5_7 && Info.Version > PakFile_Version_Latest);
            if (hasUnsupportedVersion && !UsingCustomPakVersion())
            {
                Log.Warning($"Pak file \"{Name}\" has unsupported version {(int) Info.Version}");
            }
        }

        // These games use version >= 12 to indicate their custom formats
        private bool UsingCustomPakVersion()
        {
            return Ar.Game switch
            {
                EGame.GAME_InfinityNikki or EGame.GAME_MeetYourMaker or EGame.GAME_DeadByDaylight or EGame.GAME_WutheringWaves
                    or EGame.GAME_Snowbreak or EGame.GAME_TorchlightInfinite or EGame.GAME_TowerOfFantasy
                    or EGame.GAME_TheDivisionResurgence or EGame.GAME_QQ or EGame.GAME_DreamStar
                    or EGame.GAME_EtheriaRestart or EGame.GAME_DeadByDaylight_Old or EGame.GAME_WorldofJadeDynasty => true,
                _ => false
            };
        }

        public PakFileReader(string filePath, VersionContainer? versions = null)
            : this(new FileInfo(filePath), versions) {}
        public PakFileReader(FileInfo file, VersionContainer? versions = null)
            : this(file.FullName, file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), versions) {}
        public PakFileReader(string filePath, Stream stream, VersionContainer? versions = null)
            : this(new FStreamArchive(filePath, stream, versions)) {}
        public PakFileReader(string filePath, RandomAccessStream stream, VersionContainer? versions = null)
            : this(new FRandomAccessStreamArchive(filePath, stream, versions)) {}

        public override byte[] Extract(VfsEntry entry, FByteBulkDataHeader? header = null)
        {
            if (entry is not FPakEntry pakEntry || entry.Vfs != this) throw new ArgumentException($"Wrong pak file reader, required {entry.Vfs.Name}, this is {Name}");
            // If this reader is used as a concurrent reader create a clone of the main reader to provide thread safety
            var reader = IsConcurrent ? (FArchive) Ar.Clone() : Ar;
            var alignment = pakEntry.IsEncrypted ? Aes.ALIGN : 1;

            var offset = 0;
            var requestedSize = (int) pakEntry.UncompressedSize;
            if (header is { } bulk)
            {
                offset = (int) bulk.OffsetInFile;
                requestedSize = bulk.ElementCount;
            }

            if (pakEntry.IsCompressed)
            {
#if DEBUG
                Log.Debug("{EntryName} is compressed with {CompressionMethod}", pakEntry.Name, pakEntry.CompressionMethod);
#endif
                switch (Game)
                {
                    case EGame.GAME_MarvelRivals or EGame.GAME_OperationApocalypse or EGame.GAME_WutheringWaves or EGame.GAME_MindsEye:
                        return NetEaseCompressedExtract(reader, pakEntry);
                    case EGame.GAME_GameForPeace:
                        return GameForPeaceExtract(reader, pakEntry);
                    case EGame.GAME_Rennsport:
                        return RennsportCompressedExtract(reader, pakEntry);
                    case EGame.GAME_DragonQuestXI:
                        return DQXIExtract(reader, pakEntry);
                    case EGame.GAME_ArenaBreakoutInfinite:
                        return ABIExtract(reader, pakEntry);
                }

                var compressionBlockSize = (int) pakEntry.CompressionBlockSize;
                var firstBlockIndex = offset / compressionBlockSize;
                var lastBlockIndex = (offset + requestedSize - 1) / compressionBlockSize;

                // blocks are full size, except potentially the last one
                var numBlocks = lastBlockIndex - firstBlockIndex + 1;
                var bufferSize = numBlocks * compressionBlockSize;
                if (lastBlockIndex == (int)((pakEntry.UncompressedSize - 1) / compressionBlockSize))
                {
                    var lastBlockInFileSize = (int)(pakEntry.UncompressedSize % compressionBlockSize);
                    if (lastBlockInFileSize > 0)
                        bufferSize -= compressionBlockSize - lastBlockInFileSize;
                }

                var uncompressed = new byte[bufferSize];
                var uncompressedOff = 0;

                // decompress the required blocks
                for (var blockIndex = firstBlockIndex; blockIndex <= lastBlockIndex; blockIndex++)
                {
                    var block = pakEntry.CompressionBlocks[blockIndex];
                    var blockSize = (int) block.Size;
                    var srcSize = blockSize.Align(alignment);
                    // Read the compressed block
                    var compressed = ReadAndDecryptAt(block.CompressedStart, srcSize, reader, pakEntry.IsEncrypted);
                    // Calculate the uncompressed size,
                    // its either just the compression block size,
                    // or if it's the last block, it's the remaining data size
                    var uncompressedSize = (int) Math.Min(compressionBlockSize, pakEntry.UncompressedSize - blockIndex * compressionBlockSize);
                    Decompress(compressed, 0, blockSize, uncompressed, uncompressedOff, uncompressedSize, pakEntry.CompressionMethod);
                    uncompressedOff += uncompressedSize;
                }

                var offsetInFirstBlock = offset - firstBlockIndex * compressionBlockSize;
                if (offsetInFirstBlock == 0 && requestedSize == bufferSize)
                    return uncompressed;

                var result = new byte[requestedSize];
                Array.Copy(uncompressed, offsetInFirstBlock, result, 0, requestedSize);
                return result;
            }

            switch (Game)
            {
                case EGame.GAME_MarvelRivals or EGame.GAME_OperationApocalypse or EGame.GAME_WutheringWaves or EGame.GAME_MindsEye:
                    return NetEaseExtract(reader, pakEntry);
                case EGame.GAME_Rennsport:
                    return RennsportExtract(reader, pakEntry);
                case EGame.GAME_DragonQuestXI:
                    return DQXIExtract(reader, pakEntry);
                case EGame.GAME_ArenaBreakoutInfinite:
                    return ABIExtract(reader, pakEntry);
            }

            // Pak Entry is written before the file data,
            // but it's the same as the one from the index, just without a name
            // We don't need to serialize that again so + file.StructSize

            var readOffset = offset.Align(alignment);
            var dataOffset = offset - readOffset;
            var readSize = (dataOffset + requestedSize).Align(alignment);
            var data = ReadAndDecryptAt(pakEntry.Offset + pakEntry.StructSize + readOffset, readSize, reader, pakEntry.IsEncrypted);

            if (dataOffset == 0 && requestedSize == data.Length)
                return data;

            var chunk = new byte[requestedSize];
            Array.Copy(data, dataOffset, chunk, 0, requestedSize);
            return chunk;
        }

        public override void Mount(StringComparer pathComparer)
        {
            var watch = new Stopwatch();
            watch.Start();

            if (Info.Version >= PakFile_Version_PathHashIndex)
                ReadIndexUpdated(pathComparer);
            else if (Info.IndexIsFrozen)
                ReadFrozenIndex(pathComparer);
            else
                ReadIndexLegacy(pathComparer);

            if (!IsEncrypted && EncryptedFileCount > 0)
            {
                Log.Warning($"Pak file \"{Name}\" is not encrypted but contains encrypted files");
            }

            if (Globals.LogVfsMounts)
            {
                var elapsed = watch.Elapsed;
                var sb = new StringBuilder($"Pak \"{Name}\": {FileCount} files");
                if (EncryptedFileCount > 0)
                    sb.Append($" ({EncryptedFileCount} encrypted)");
                if (MountPoint.Contains("/"))
                    sb.Append($", mount point: \"{MountPoint}\"");
                sb.Append($", order {ReadOrder}");
                sb.Append($", version {(int) Info.Version} in {elapsed}");
                Log.Information(sb.ToString());
            }
        }

        private void ReadIndexLegacy(StringComparer pathComparer)
        {
            Ar.Position = Info.IndexOffset;
            var index = new FByteArchive($"{Name} - Index", ReadAndDecryptIndex((int) Info.IndexSize), Versions);

            string mountPoint;
            try
            {
                mountPoint = index.ReadFString();
            }
            catch (Exception e)
            {
                throw new InvalidAesKeyException($"Given aes key '{AesKey?.KeyString}' is not working with '{Name}'", e);
            }

            ValidateMountPoint(ref mountPoint);
            MountPoint = mountPoint;

            if (Ar.Game == EGame.GAME_GameForPeace)
            {
                GameForPeaceReadIndex(pathComparer, index);
                return;
            }
            if (Ar.Game == EGame.GAME_DragonQuestXI)
            {
                DQXIReadIndexLegacy(pathComparer, index);
                return;
            }

            var fileCount = index.Read<int>();
            if (Ar.Game == EGame.GAME_TransformersOnline) fileCount -= 100;

            var files = new Dictionary<string, GameFile>(fileCount, pathComparer);
            for (var i = 0; i < fileCount; i++)
            {
                var path = string.Concat(mountPoint, index.ReadFString());
                var entry = new FPakEntry(this, path, index);
                if (entry is { IsDeleted: true, Size: 0 }) continue;
                if (entry.IsEncrypted) EncryptedFileCount++;
                files[path] = entry;
            }

            Files = files;
        }

        private void ReadIndexUpdated(StringComparer pathComparer)
        {
            if (Ar.Game == EGame.GAME_CrystalOfAtlan)
            {
                CoAReadIndexUpdated(pathComparer);
                return;
            }

            // Prepare primary index and decrypt if necessary
            Ar.Position = Info.IndexOffset;
            using FArchive primaryIndex = new FByteArchive($"{Name} - Primary Index", ReadAndDecryptIndex((int) Info.IndexSize));

            int fileCount = 0;
            EncryptedFileCount = 0;

            if (Ar.Game is EGame.GAME_DreamStar or EGame.GAME_DeltaForceHawkOps)
            {
                primaryIndex.Position += 8; // PathHashSeed
                fileCount = primaryIndex.Read<int>();
            }

            string mountPoint;
            try
            {
                mountPoint = primaryIndex.ReadFString();
            }
            catch (Exception e)
            {
                throw new InvalidAesKeyException($"Given aes key '{AesKey?.KeyString}' is not working with '{Name}'", e);
            }

            ValidateMountPoint(ref mountPoint);
            MountPoint = mountPoint;

            if (!(Ar.Game is EGame.GAME_DreamStar or EGame.GAME_DeltaForceHawkOps))
            {
                fileCount = primaryIndex.Read<int>();
                primaryIndex.Position += 8; // PathHashSeed
            }

            if (!primaryIndex.ReadBoolean())
                throw new ParserException(primaryIndex, "No path hash index");

            primaryIndex.Position += 36; // PathHashIndexOffset (long) + PathHashIndexSize (long) + PathHashIndexHash (20 bytes)
            if (Ar.Game == EGame.GAME_Rennsport) primaryIndex.Position += 16;

            if (!primaryIndex.ReadBoolean())
                throw new ParserException(primaryIndex, "No directory index");

            if (Ar.Game == EGame.GAME_TheDivisionResurgence) primaryIndex.Position += 40; // duplicate entry

            var directoryIndexOffset = primaryIndex.Read<long>();
            var directoryIndexSize = primaryIndex.Read<long>();
            primaryIndex.Position += 20; // Directory Index hash
            if (Ar.Game == EGame.GAME_Rennsport) primaryIndex.Position += 20;
            var encodedPakEntriesSize = primaryIndex.Read<int>();
            if (Ar.Game == EGame.GAME_Rennsport)
            {
                primaryIndex.Position -= 4;
                encodedPakEntriesSize = (int) (primaryIndex.Length - primaryIndex.Position - 6);
            }

            var encodedPakEntriesData = primaryIndex.ReadBytes(encodedPakEntriesSize);
            using var encodedPakEntries = new GenericBufferReader(encodedPakEntriesData);

            var FilesNum = primaryIndex.Read<int>();
            if (FilesNum < 0)
                throw new ParserException("Corrupt pak PrimaryIndex detected");

            var NonEncodedEntries = primaryIndex.ReadArray(FilesNum, () => new FPakEntry(this, "", primaryIndex));

            // Read FDirectoryIndex
            Ar.Position = directoryIndexOffset;
            var data = Ar.Game switch
            {
                EGame.GAME_Rennsport => RennsportAes.RennsportDecrypt(Ar.ReadBytes((int) directoryIndexSize), 0, (int) directoryIndexSize, true, this, true),
                _ => ReadAndDecryptIndex((int) directoryIndexSize),
            };

            using var directoryIndex = new GenericBufferReader(data);

            var files = new Dictionary<string, GameFile>(fileCount, pathComparer);

            const int poolLength = 256;
            var mountPointSpan = MountPoint.AsSpan();
            using var charsPool = SpanOwner<char>.Allocate(poolLength * 2);
            var charsSpan = charsPool.Span;
            var dirPoolSpan = charsSpan[..poolLength];
            var fileNamePoolSpan = charsSpan[poolLength..];
            var directoryIndexLength = directoryIndex.Read<int>();
            for (var dirIndex = 0; dirIndex < directoryIndexLength; dirIndex++)
            {
                var dirSpan = dirPoolSpan;
                var dir = directoryIndex.ReadFStringMemory();
                var dirLength = dir.GetEncoding().GetChars(dir.GetSpan(), dirSpan);
                var trimDir = !mountPointSpan.IsEmpty && dirSpan[0] == '/' && mountPointSpan[^1] == '/';
                dirSpan = dirSpan[(trimDir ? 1 : 0)..dirLength];

                var fileEntries = directoryIndex.Read<int>();
                for (var fileIndex = 0; fileIndex < fileEntries; fileIndex++)
                {
                    var fileNameSpan = fileNamePoolSpan;
                    var fileName = directoryIndex.ReadFStringMemory(); // supports PakFile_Version_Utf8PakDirectory too
                    var fileNameLength = fileName.GetEncoding().GetChars(fileName.GetSpan(), fileNameSpan);
                    fileNameSpan = fileNameSpan[..fileNameLength];
                    var path = string.Concat(mountPointSpan, dirSpan, fileNameSpan);

                    var offset = directoryIndex.Read<int>();
                    if (offset == int.MinValue) continue;

                    FPakEntry entry;
                    if (offset >= 0)
                    {
                        entry = new FPakEntry(this, path, encodedPakEntries, offset);
                    }
                    else
                    {
                        var index = -offset - 1;
                        if (index <0 || index >= NonEncodedEntries.Length)
                        {
                            Log.Warning("Invalid nonencoded pak entry with index {Index}, path {Path}", index, path);
                            continue;
                        }

                        entry = NonEncodedEntries[index];
                        entry.Path = path;
                    }
                    if (entry.IsEncrypted) EncryptedFileCount++;
                    files[path] = entry;
                }
            }

            Files = files;
        }

        private void ReadFrozenIndex(StringComparer pathComparer)
        {
            this.Ar.Position = Info.IndexOffset;
            var Ar = new FMemoryImageArchive(new FByteArchive("FPakFileData", this.Ar.ReadBytes((int) Info.IndexSize)), 8);

            var mountPoint = Ar.ReadFString();
            ValidateMountPoint(ref mountPoint);
            MountPoint = mountPoint;

            var entries = Ar.ReadArray(() => new FPakEntry(this, Ar));

            // read TMap<FString, TMap<FString, int32>>
            var index = Ar.ReadTMap(
                () => Ar.ReadFString(),
                () => Ar.ReadTMap(
                    () => Ar.ReadFString(),
                    () => Ar.Read<int>(),
                    16, 4
                ),
                16, 56
            );

            var files = new Dictionary<string, GameFile>(entries.Length, pathComparer);
            foreach (var (dir, dirContents) in index)
            {
                foreach (var (name, fileIndex) in dirContents)
                {
                    string path;
                    if (mountPoint.EndsWith('/') && dir.StartsWith('/'))
                        path = dir.Length == 1 ? string.Concat(mountPoint, name) : string.Concat(mountPoint, dir[1..], name);
                    else
                        path = string.Concat(mountPoint, dir, name);

                    var entry = entries[fileIndex];
                    entry.Path = path;

                    if (entry is { IsDeleted: true, Size: 0 }) continue;
                    if (entry.IsEncrypted) EncryptedFileCount++;
                    files[path] = entry;
                }
            }

            Files = files;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override byte[] ReadAndDecrypt(int length) => ReadAndDecrypt(length, Ar, IsEncrypted);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override byte[] ReadAndDecryptIndex(int length) => ReadAndDecryptIndex(length, Ar, IsEncrypted);

        public override byte[] MountPointCheckBytes()
        {
            var reader = IsConcurrent ? (FArchive) Ar.Clone() : Ar;
            reader.Position = Info.IndexOffset;
            var size = Math.Min((int) Info.IndexSize, 4 + MAX_MOUNTPOINT_TEST_LENGTH * 2);
            return reader.ReadBytes(size.Align(Aes.ALIGN));
        }

        public override void Dispose()
        {
            Ar.Dispose();
        }
    }
}
