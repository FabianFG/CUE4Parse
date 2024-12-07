using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.GameTypes.Rennsport.Encryption.Aes;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;

using OffiUtils;

using Serilog;

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
            if (Info.Version > PakFile_Version_Latest &&
                Ar.Game != EGame.GAME_TowerOfFantasy && Ar.Game != EGame.GAME_MeetYourMaker &&
                Ar.Game != EGame.GAME_Snowbreak && Ar.Game != EGame.GAME_TheDivisionResurgence &&
                Ar.Game != EGame.GAME_TorchlightInfinite && Ar.Game != EGame.GAME_DeadByDaylight &&
                Ar.Game != EGame.GAME_QQ && Ar.Game != EGame.GAME_DreamStar) // These games use version >= 12 to indicate their custom formats
            {
                log.Warning($"Pak file \"{Name}\" has unsupported version {(int) Info.Version}");
            }
        }

        public PakFileReader(string filePath, VersionContainer? versions = null)
            : this(new FileInfo(filePath), versions) {}
        public PakFileReader(FileInfo file, VersionContainer? versions = null)
            : this(file.FullName, file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), versions) {}
        public PakFileReader(string filePath, Stream stream, VersionContainer? versions = null)
            : this(new FStreamArchive(filePath, stream, versions)) {}
        public PakFileReader(string filePath, RandomAccessStream stream, VersionContainer? versions = null)
            : this(new FRandomAccessStreamArchive(filePath, stream, versions)) {}

        public override byte[] Extract(VfsEntry entry)
        {
            if (entry is not FPakEntry pakEntry || entry.Vfs != this) throw new ArgumentException($"Wrong pak file reader, required {entry.Vfs.Name}, this is {Name}");
            // If this reader is used as a concurrent reader create a clone of the main reader to provide thread safety
            var reader = IsConcurrent ? (FArchive) Ar.Clone() : Ar;
            if (pakEntry.IsCompressed)
            {
#if DEBUG
                Log.Debug("{EntryName} is compressed with {CompressionMethod}", pakEntry.Name, pakEntry.CompressionMethod);
#endif
                switch (Game)
                {
                    case EGame.GAME_MarvelRivals or EGame.GAME_OperationApocalypse:
                        return NetEaseCompressedExtract(reader, pakEntry);
                    case EGame.GAME_GameForPeace:
                        return GameForPeaceExtract(reader, pakEntry);
                    case EGame.GAME_Rennsport:
                        return RennsportCompressedExtract(reader, pakEntry);
                }

                var uncompressed = new byte[(int) pakEntry.UncompressedSize];
                var uncompressedOff = 0;
                foreach (var block in pakEntry.CompressionBlocks)
                {
                    var blockSize = (int) block.Size;
                    var srcSize = blockSize.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1);
                    // Read the compressed block
                    var compressed = ReadAndDecryptAt(block.CompressedStart, srcSize, reader, pakEntry.IsEncrypted);
                    // Calculate the uncompressed size,
                    // its either just the compression block size,
                    // or if it's the last block, it's the remaining data size
                    var uncompressedSize = (int) Math.Min(pakEntry.CompressionBlockSize, pakEntry.UncompressedSize - uncompressedOff);
                    Decompress(compressed, 0, blockSize, uncompressed, uncompressedOff, uncompressedSize, pakEntry.CompressionMethod);
                    uncompressedOff += (int) pakEntry.CompressionBlockSize;
                }

                return uncompressed;
            }

            switch (Game)
            {
                case EGame.GAME_MarvelRivals or EGame.GAME_OperationApocalypse:
                    return NetEaseExtract(reader, pakEntry);
                case EGame.GAME_Rennsport:
                    return RennsportExtract(reader, pakEntry);
            }

            // Pak Entry is written before the file data,
            // but it's the same as the one from the index, just without a name
            // We don't need to serialize that again so + file.StructSize
            var size = (int) pakEntry.UncompressedSize.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1);
            var data = ReadAndDecryptAt(pakEntry.Offset + pakEntry.StructSize /* Doesn't seem to be the case with older pak versions */,
                size, reader, pakEntry.IsEncrypted);
            return size != pakEntry.UncompressedSize ? data.SubByteArray((int) pakEntry.UncompressedSize) : data;
        }

        public override IReadOnlyDictionary<string, GameFile> Mount(bool caseInsensitive = false)
        {
            var watch = new Stopwatch();
            watch.Start();

            if (Info.Version >= PakFile_Version_PathHashIndex)
                ReadIndexUpdated(caseInsensitive);
            else if (Info.IndexIsFrozen)
                ReadFrozenIndex(caseInsensitive);
            else
                ReadIndexLegacy(caseInsensitive);

            if (!IsEncrypted && EncryptedFileCount > 0)
            {
                log.Warning($"Pak file \"{Name}\" is not encrypted but contains encrypted files");
            }

            if (Globals.LogVfsMounts)
            {
                var elapsed = watch.Elapsed;
                var sb = new StringBuilder($"Pak \"{Name}\": {FileCount} files");
                if (EncryptedFileCount > 0)
                    sb.Append($" ({EncryptedFileCount} encrypted)");
                if (MountPoint.Contains("/"))
                    sb.Append($", mount point: \"{MountPoint}\"");
                sb.Append($", version {(int) Info.Version} in {elapsed}");
                log.Information(sb.ToString());
            }

            return Files;
        }

        private void ReadIndexLegacy(bool caseInsensitive)
        {
            Ar.Position = Info.IndexOffset;
            var index = new FByteArchive($"{Name} - Index", ReadAndDecrypt((int) Info.IndexSize), Versions);

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
                GameForPeaceReadIndex(caseInsensitive, index);
                return;
            }

            var fileCount = index.Read<int>();
            var files = new Dictionary<string, GameFile>(fileCount);

            for (var i = 0; i < fileCount; i++)
            {
                var path = string.Concat(mountPoint, index.ReadFString());
                var entry = new FPakEntry(this, path, index);
                if (entry is { IsDeleted: true, Size: 0 })
                    continue;
                if (entry.IsEncrypted)
                    EncryptedFileCount++;
                if (caseInsensitive)
                    files[path.ToLowerInvariant()] = entry;
                else
                    files[path] = entry;
            }

            Files = files;
        }

        private void ReadIndexUpdated(bool caseInsensitive)
        {
            // Prepare primary index and decrypt if necessary
            Ar.Position = Info.IndexOffset;
            FArchive primaryIndex = new FByteArchive($"{Name} - Primary Index", ReadAndDecrypt((int) Info.IndexSize));

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
            var encodedPakEntries = primaryIndex.ReadBytes(encodedPakEntriesSize);

            if (primaryIndex.Read<int>() < 0)
                throw new ParserException("Corrupt pak PrimaryIndex detected");

            // Read FDirectoryIndex
            Ar.Position = directoryIndexOffset;
            var directoryIndex = new FByteArchive($"{Name} - Directory Index", ReadAndDecrypt((int) directoryIndexSize));
            if (Ar.Game == EGame.GAME_Rennsport)
            {
                Ar.Position = directoryIndexOffset;
                directoryIndex = new FByteArchive($"{Name} - Directory Index",
                    RennsportAes.RennsportDecrypt(Ar.ReadBytes((int) directoryIndexSize), 0,
                        (int) directoryIndexSize, true, this, true));
            }
            var directoryIndexLength = directoryIndex.Read<int>();
            var files = new Dictionary<string, GameFile>(fileCount);

            unsafe
            {
                fixed (byte* ptr = encodedPakEntries)
                {
                    for (var i = 0; i < directoryIndexLength; i++)
                    {
                        var dir = directoryIndex.ReadFString();
                        var dirDictLength = directoryIndex.Read<int>();

                        for (var j = 0; j < dirDictLength; j++)
                        {
                            var name = directoryIndex.ReadFString();
                            string path;
                            if (mountPoint.EndsWith('/') && dir.StartsWith('/'))
                                path = dir.Length == 1 ? string.Concat(mountPoint, name) : string.Concat(mountPoint, dir[1..], name);
                            else
                                path = string.Concat(mountPoint, dir, name);

                            var offset = directoryIndex.Read<int>();
                            if (offset == int.MinValue) continue;

                            var entry = new FPakEntry(this, path, ptr + offset);
                            if (entry.IsEncrypted)
                                EncryptedFileCount++;
                            if (caseInsensitive)
                                files[path.ToLowerInvariant()] = entry;
                            else
                                files[path] = entry;
                        }
                    }
                }
            }

            Files = files;
        }

        private void ReadFrozenIndex(bool caseInsensitive)
        {
            this.Ar.Position = Info.IndexOffset;
            var Ar = new FMemoryImageArchive(new FByteArchive("FPakFileData", this.Ar.ReadBytes((int) Info.IndexSize)), 8);

            var mountPoint = Ar.ReadFString();
            ValidateMountPoint(ref mountPoint);
            MountPoint = mountPoint;

            var entries = Ar.ReadArray(() => new FPakEntry(this, Ar));
            var fileCount = entries.Length;
            var files = new Dictionary<string, GameFile>(fileCount);

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
                    if (entry.IsDeleted && entry.Size == 0)
                        continue;
                    if (entry.IsEncrypted)
                        EncryptedFileCount++;
                    if (caseInsensitive)
                        files[path.ToLowerInvariant()] = entry;
                    else
                        files[path] = entry;
                }
            }

            Files = files;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override byte[] ReadAndDecrypt(int length) => ReadAndDecrypt(length, Ar, IsEncrypted);

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