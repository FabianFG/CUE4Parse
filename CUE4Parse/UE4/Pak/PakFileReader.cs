using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.Vfs;
using CUE4Parse.Utils;
using Serilog;
using static CUE4Parse.Compression.Compression;
using static CUE4Parse.UE4.Pak.Objects.EPakFileVersion;

namespace CUE4Parse.UE4.Pak
{
    public class PakFileReader : AbstractAesVfsReader
    {
        public readonly FArchive Ar;

        public readonly FPakInfo Info;

        public override int ReadOrder { get; } = 0;
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
            if (Info.Version > PakFile_Version_Latest)
            {
                log.Warning($"Pak file \"{Name}\" has unsupported version {(int) Info.Version}");
            }

            if (Path.EndsWith("_P.pak"))
            {
                // Prioritize based on the chunk version number
                // Default to version 1 for single patch system
                var chunkVersionNumber = 1;
                var versionString = Path.SubstringBeforeLast("_").SubstringAfterLast("_");
                if (int.TryParse(versionString, out int chunkVersionSigned) && chunkVersionSigned >= 1)
                {
                    // Increment by one so that the first patch file still gets more priority than the base pak file
                    chunkVersionNumber = chunkVersionSigned + 1;
                }
                ReadOrder += 100 * chunkVersionNumber;
            }
        }

        public PakFileReader(string filePath, VersionContainer? versions = null)
            : this(new FileInfo(filePath), versions) {}
        public PakFileReader(FileInfo file, VersionContainer? versions = null)
            : this(file.FullName, file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), versions) {}
        public PakFileReader(string filePath, Stream stream, VersionContainer? versions = null)
            : this(new FStreamArchive(filePath, stream, versions)) {}

        public override byte[] Extract(VfsEntry entry)
        {
            if (entry is not FPakEntry pakEntry || entry.Vfs != this) throw new ArgumentException($"Wrong pak file reader, required {entry.Vfs.Name}, this is {Name}");
            // If this reader is used as a concurrent reader create a clone of the main reader to provide thread safety
            var reader = IsConcurrent ? (FArchive) Ar.Clone() : Ar;
            if (pakEntry.IsCompressed)
            {
#if DEBUG
                Log.Debug($"{pakEntry.Name} is compressed with {pakEntry.CompressionMethod}");
#endif
                var uncompressed = new byte[(int) pakEntry.UncompressedSize];
                var uncompressedOff = 0;
                foreach (var block in pakEntry.CompressionBlocks)
                {
                    reader.Position = block.CompressedStart;
                    var blockSize = (int) block.Size;
                    var srcSize = blockSize.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1);
                    // Read the compressed block
                    byte[] compressed = ReadAndDecrypt(srcSize, reader, pakEntry.IsEncrypted);
                    // Calculate the uncompressed size,
                    // its either just the compression block size
                    // or if its the last block its the remaining data size
                    var uncompressedSize = (int) Math.Min(pakEntry.CompressionBlockSize, pakEntry.UncompressedSize - uncompressedOff);
                    Decompress(compressed, 0, blockSize, uncompressed, uncompressedOff, uncompressedSize, pakEntry.CompressionMethod);
                    uncompressedOff += (int) pakEntry.CompressionBlockSize;
                }

                return uncompressed;
            }

            // Pak Entry is written before the file data,
            // but its the same as the one from the index, just without a name
            // We don't need to serialize that again so + file.StructSize
            reader.Position = pakEntry.Offset + pakEntry.StructSize; // Doesn't seem to be the case with older pak versions
            var size = (int) pakEntry.UncompressedSize.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1);
            var data = ReadAndDecrypt(size, reader, pakEntry.IsEncrypted);
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

        private IReadOnlyDictionary<string, GameFile> ReadIndexLegacy(bool caseInsensitive)
        {
            Ar.Position = Info.IndexOffset;
            var index = new FByteArchive($"{Name} - Index", ReadAndDecrypt((int) Info.IndexSize));

            string mountPoint;
            try
            {
                mountPoint = index.ReadFString();
            }
            catch (Exception e)
            {
                throw new InvalidAesKeyException($"Given aes key '{AesKey?.KeyString}'is not working with '{Name}'", e);
            }

            ValidateMountPoint(ref mountPoint);
            MountPoint = mountPoint;
            var fileCount = index.Read<int>();
            var files = new Dictionary<string, GameFile>(fileCount);

            for (var i = 0; i < fileCount; i++)
            {
                var path = string.Concat(mountPoint, index.ReadFString());
                var entry = new FPakEntry(this, path, index);
                if (entry.IsDeleted && entry.Size == 0)
                    continue;
                if (entry.IsEncrypted)
                    EncryptedFileCount++;
                if (caseInsensitive)
                    files[path.ToLowerInvariant()] = entry;
                else
                    files[path] = entry;
            }

            return Files = files;
        }

        private IReadOnlyDictionary<string, GameFile> ReadIndexUpdated(bool caseInsensitive)
        {
            // Prepare primary index and decrypt if necessary
            Ar.Position = Info.IndexOffset;
            FArchive primaryIndex = new FByteArchive($"{Name} - Primary Index", ReadAndDecrypt((int) Info.IndexSize));

            string mountPoint;
            try
            {
                mountPoint = primaryIndex.ReadFString();
            }
            catch (Exception e)
            {
                throw new InvalidAesKeyException($"Given aes key '{AesKey?.KeyString}'is not working with '{Name}'", e);
            }

            ValidateMountPoint(ref mountPoint);
            MountPoint = mountPoint;

            var fileCount = primaryIndex.Read<int>();
            EncryptedFileCount = 0;

            primaryIndex.Position += 8; // PathHashSeed

            if (!primaryIndex.ReadBoolean())
                throw new ParserException(primaryIndex, "No path hash index");

            primaryIndex.Position += 36; // PathHashIndexOffset (long) + PathHashIndexSize (long) + PathHashIndexHash (20 bytes)

            if (!primaryIndex.ReadBoolean())
                throw new ParserException(primaryIndex, "No directory index");

            var directoryIndexOffset = primaryIndex.Read<long>();
            var directoryIndexSize = primaryIndex.Read<long>();
            primaryIndex.Position += 20; // Directory Index hash
            var encodedPakEntriesSize = primaryIndex.Read<int>();
            var encodedPakEntries = primaryIndex.ReadBytes(encodedPakEntriesSize);

            if (primaryIndex.Read<int>() < 0)
                throw new ParserException("Corrupt pak PrimaryIndex detected");

            // Read FDirectoryIndex
            Ar.Position = directoryIndexOffset;
            var directoryIndex = new FByteArchive($"{Name} - Directory Index", ReadAndDecrypt((int) directoryIndexSize));
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

                            var entry = new FPakEntry(this, path, ptr + directoryIndex.Read<int>());
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

            return Files = files;
        }

        private IReadOnlyDictionary<string, GameFile> ReadFrozenIndex(bool caseInsensitive)
        {
            this.Ar.Position = Info.IndexOffset;
            var Ar = new FMemoryImageArchive(new FByteArchive("FPakFileData", this.Ar.ReadBytes((int) Info.IndexSize)));

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

            return Files = files;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override byte[] ReadAndDecrypt(int length) => ReadAndDecrypt(length, Ar, IsEncrypted);

        public override byte[] MountPointCheckBytes()
        {
            var reader = IsConcurrent ? (FArchive) Ar.Clone() : Ar;
            reader.Position = Info.IndexOffset;
            return reader.ReadBytes((4 + MAX_MOUNTPOINT_TEST_LENGTH * 2).Align(Aes.ALIGN));
        }

        public override void Dispose()
        {
            Ar.Dispose();
        }
    }
}