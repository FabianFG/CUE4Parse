using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.GameTypes.ABI.Encryption.SM4;
using CUE4Parse.GameTypes.LordOfMysteries.UE4.Lua;
using CUE4Parse.GameTypes.Netmarble.NiNoKuni.UE4.Encryption;
using CUE4Parse.GameTypes.NFS.Mobile.Lua;
using CUE4Parse.GameTypes.NTE.Encryption;
using CUE4Parse.GameTypes.PUBG.UE4.Lua;
using CUE4Parse.GameTypes.Rennsport.Encryption.Aes;
using CUE4Parse.GameTypes.RocoKingdomWorld.Lua;
using CUE4Parse.GameTypes.Snowbreak.Encryption.Lua;
using CUE4Parse.GameTypes.Strinova.Lua;
using CUE4Parse.GameTypes.Tencent.ValorantSource.Lua;
using CUE4Parse.GameTypes.UDWN.Lua;
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

namespace CUE4Parse.UE4.Pak;

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
        CompressionMethods = [.. Info.CompressionMethods];

        var hasUnsupportedVersion = (Ar.Game < GAME_UE5_7 && Info.Version > PakFile_Version_Fnv64BugFix)
                                    || (Ar.Game >= GAME_UE5_7 && Info.Version > PakFile_Version_Latest);
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
            GAME_InfinityNikki or GAME_MeetYourMaker or GAME_DeadByDaylight or GAME_WutheringWaves
                or GAME_Snowbreak or GAME_TorchlightInfinite or GAME_TowerOfFantasy
                or GAME_TheDivisionResurgence or GAME_QQ or GAME_DreamStar
                or GAME_EtheriaRestart or GAME_DeadByDaylight_Old or GAME_WorldofJadeDynasty
                or GAME_EmbersofTheUncrowned or GAME_ValorantSource => true,
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

        long offset = 0;
        var requestedSize = (int) pakEntry.UncompressedSize;
        if (header is { } bulk)
        {
            offset = bulk.OffsetInFile;
            requestedSize = (int) bulk.SizeOnDisk;
        }

        if (pakEntry.IsCompressed)
        {
            switch (Game)
            {
                case GAME_MarvelRivals or GAME_OperationApocalypse or GAME_WutheringWaves or GAME_MindsEye:
                    return PartialEncryptCompressedExtract(reader, pakEntry, header);
                case GAME_GameForPeace:
                    return GameForPeaceExtract(reader, pakEntry);
                case GAME_Rennsport:
                    return RennsportCompressedExtract(reader, pakEntry);
                case GAME_DragonQuestXI:
                    return DQXIExtract(reader, pakEntry);
                case GAME_CenturyAgeofAshes when pakEntry.CompressionMethod is CompressionMethod.PWC:
                    return CenturyExtract(reader, pakEntry);
                case GAME_ArenaBreakoutInfinite or GAME_ArenaBreakoutMobile when header is null || ABIDecryption.encryptedFiles.Contains(pakEntry.Extension, StringComparer.OrdinalIgnoreCase):
                    return ABIExtract(reader, pakEntry);
                case GAME_eBaseballProSpirit:
                    return ProSpiExtract(reader, pakEntry, alignment, header, offset, requestedSize);
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

            var compressedBuffer = Array.Empty<byte>();
            // decompress the required blocks
            for (var blockIndex = firstBlockIndex; blockIndex <= lastBlockIndex; blockIndex++)
            {
                var block = pakEntry.CompressionBlocks[blockIndex];
                var blockSize = (int) block.Size;
                var srcSize = blockSize.Align(alignment);
                if (srcSize > compressedBuffer.Length)
                {
                    compressedBuffer = new byte[srcSize];
                }
                // Read the compressed block
                var compressed = ReadAndDecryptAt(compressedBuffer, block.CompressedStart, srcSize, reader, pakEntry.IsEncrypted);
                // Calculate the uncompressed size,
                // its either just the compression block size,
                // or if it's the last block, it's the remaining data size
                var uncompressedSize = (int) Math.Min(compressionBlockSize, pakEntry.UncompressedSize - blockIndex * compressionBlockSize);
                Decompress(compressed, 0, blockSize, uncompressed, uncompressedOff, uncompressedSize, pakEntry.CompressionMethod);
                uncompressedOff += uncompressedSize;
            }

            switch (Ar.Game)
            {
                case GAME_RocoKingdomWorld when pakEntry.Extension is "luac":
                    return NRCLua.DecryptLuaBytecode(pakEntry.Path, uncompressed);
                case GAME_NevernessToEverness when pakEntry.Extension is "ini":
                    return NevernessToEvernessIniEncryption.DecryptIni(uncompressed, requestedSize);
                case GAME_Snowbreak when pakEntry.Extension is "lua":
                    return SnowbreakLua.DecryptLua(uncompressed, requestedSize);
                case GAME_Undawn when pakEntry.Extension is "lua":
                    return UndawnLua.DecryptLuaBytecode(pakEntry.Path, uncompressed);
                case GAME_Strinova when pakEntry.Extension is "lua":
                    uncompressed = StrinovaLua.DecryptLuaBytecode(uncompressed);
                    break;
                case GAME_NeedForSpeedMobile when pakEntry.Extension is "lua":
                    return NFSLua.RestoreLuaBytecode(pakEntry.Path, uncompressed);
                case GAME_LordOfMysteries when pakEntry.Extension is "luac":
                    return LoMLua.DecryptLuaJITBytecode(pakEntry.Path, uncompressed);
                case GAME_NiNoKuniCrossWorlds when pakEntry.Extension is "csv":
                    return NiNoKuniCsv.DecryptCsv(pakEntry.Name, uncompressed);
                case GAME_ValorantSource when pakEntry.Extension is "lua":
                    return ValorantSourceLua.DecryptLuaBytecode(pakEntry.Name, uncompressed);
                default:
                    break;
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
            case GAME_MarvelRivals or GAME_OperationApocalypse or GAME_WutheringWaves or GAME_MindsEye:
                return PartialEncryptExtract(reader, pakEntry, header);
            case GAME_Rennsport:
                return RennsportExtract(reader, pakEntry);
            case GAME_DragonQuestXI:
                return DQXIExtract(reader, pakEntry);
            case GAME_ArenaBreakoutInfinite or GAME_ArenaBreakoutMobile when header is null || ABIDecryption.encryptedFiles.Contains(pakEntry.Extension, StringComparer.OrdinalIgnoreCase):
                return ABIExtract(reader, pakEntry);
            case GAME_eBaseballProSpirit:
                return ProSpiExtract(reader, pakEntry, alignment, header, offset, requestedSize);
        }

        // Pak Entry is written before the file data,
        // but it's the same as the one from the index, just without a name
        // We don't need to serialize that again so + file.StructSize

        var readOffset = offset & ~((long) alignment - 1);
        var dataOffset = offset - readOffset;
        var readSize = (dataOffset + requestedSize).Align(alignment);
        var data = ReadAndDecryptAt(pakEntry.Offset + pakEntry.StructSize + readOffset, (int) readSize, reader, pakEntry.IsEncrypted);

        switch (Ar.Game)
        {
            case GAME_RocoKingdomWorld when pakEntry.Extension is "luac":
                return NRCLua.DecryptLuaBytecode(pakEntry.Path, data);
            case GAME_NevernessToEverness when pakEntry.Extension is "ini":
                return NevernessToEvernessIniEncryption.DecryptIni(data, requestedSize);
            case GAME_Snowbreak when pakEntry.Extension is "lua":
                return SnowbreakLua.DecryptLua(data, requestedSize);
            case GAME_GameForPeace when pakEntry.Extension is "lua":
                return GameForPeaceLua.DecryptLuaBytecode(pakEntry.Path, data);
            case GAME_Undawn when pakEntry.Extension is "lua":
                return UndawnLua.DecryptLuaBytecode(pakEntry.Path, data);
            case GAME_Strinova when pakEntry.Extension is "lua":
                data = StrinovaLua.DecryptLuaBytecode(data);
                break;
            case GAME_NeedForSpeedMobile when pakEntry.Extension is "lua":
                return NFSLua.RestoreLuaBytecode(pakEntry.Path, data);
            case GAME_LordOfMysteries when pakEntry.Extension is "luac":
                return LoMLua.DecryptLuaJITBytecode(pakEntry.Path, data);
            case GAME_NiNoKuniCrossWorlds when pakEntry.Extension is "csv":
                return NiNoKuniCsv.DecryptCsv(pakEntry.Name, data);
            case GAME_ValorantSource when pakEntry.Extension is "lua":
                return ValorantSourceLua.DecryptLuaBytecode(pakEntry.Name, data);
            default:
                break;
        }

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
        {
            switch (Game)
            {
                case GAME_CrystalOfAtlan:
                    CoAReadIndexUpdated(pathComparer);
                    break;
                case GAME_DragonSwordAwakening:
                    DragonSwordReadIndexUpdated(pathComparer);
                    break;
                case GAME_ValorantSource:
                    ValorantSourceReadIndexUpdated(pathComparer);
                    break;
                default:
                    ReadIndexUpdated(pathComparer);
                    break;
            }
        }
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
            if (MountPoint.Contains('/'))
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

        if (Ar.Game == GAME_GameForPeace)
        {
            GameForPeaceReadIndex(pathComparer, index);
            return;
        }
        if (Ar.Game == GAME_DragonQuestXI)
        {
            DQXIReadIndexLegacy(pathComparer, index);
            return;
        }

        var fileCount = index.Read<int>();
        if (Ar.Game == GAME_TransformersOnline) fileCount -= 100;

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
        // Prepare primary index and decrypt if necessary
        Ar.Position = Info.IndexOffset;
        using FArchive primaryIndex = new FByteArchive($"{Name} - Primary Index", ReadAndDecryptIndex((int) Info.IndexSize));

        var fileCount = 0;
        EncryptedFileCount = 0;

        if (Ar.Game is GAME_DreamStar or GAME_DeltaForce)
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

        if (Ar.Game is not (GAME_DreamStar or GAME_DeltaForce))
        {
            fileCount = primaryIndex.Read<int>();
            primaryIndex.Position += 8; // PathHashSeed
        }

        if (!primaryIndex.ReadBoolean())
            throw new ParserException(primaryIndex, "No path hash index");

        primaryIndex.Position += 36; // PathHashIndexOffset (long) + PathHashIndexSize (long) + PathHashIndexHash (20 bytes)
        if (Ar.Game == GAME_Rennsport) primaryIndex.Position += 16;

        if (!primaryIndex.ReadBoolean())
            throw new ParserException(primaryIndex, "No directory index");

        if (Ar.Game == GAME_TheDivisionResurgence) primaryIndex.Position += 40; // duplicate entry

        var directoryIndexOffset = primaryIndex.Read<long>();
        var directoryIndexSize = primaryIndex.Read<long>();
        primaryIndex.Position += 20; // Directory Index hash
        if (Ar.Game == GAME_Rennsport) primaryIndex.Position += 20;
        var encodedPakEntriesSize = primaryIndex.Read<int>();
        if (Ar.Game == GAME_Rennsport)
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
            GAME_Rennsport => RennsportAes.RennsportDecrypt(Ar.ReadBytes((int) directoryIndexSize), 0, (int) directoryIndexSize, true, this, true),
            _ => ReadAndDecryptIndex((int) directoryIndexSize),
        };

        using var directoryIndex = new GenericBufferReader(data);

        var files = new Dictionary<string, GameFile>(fileCount, pathComparer);

        if (Info.Version >= PakFile_Version_SortedDirectoryIndex && Ar.Game >= GAME_UE5_9)
        {
            ReadFlatDirectoryIndex(directoryIndex, files, encodedPakEntries, NonEncodedEntries);
            Files = files;
            return;
        }

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

    private void ReadFlatDirectoryIndex(
        GenericBufferReader directoryIndex, Dictionary<string, GameFile> files,
        GenericBufferReader encodedPakEntries, FPakEntry[] nonEncodedEntries
    )
    {
        const int flatMagic = 0x50464451; // 'PFDQ'
        if (directoryIndex.Read<int>() != flatMagic)
            throw new ParserException("Corrupt pak FullDirectoryIndex (flat) detected");

        var numDirs = directoryIndex.Read<int>();
        var numFiles = directoryIndex.Read<int>();
        var restartInterval = directoryIndex.Read<int>();
        var dirBlobBytes = directoryIndex.Read<int>();
        var fileBlobBytes = directoryIndex.Read<int>();
        var numPathHashes = directoryIndex.Read<int>();
        directoryIndex.Position += sizeof(int); // pad that 8-aligns the following uint64 hash table

        if (numDirs < 0 || numFiles < 0 || restartInterval <= 0 || dirBlobBytes < 0 || fileBlobBytes < 0 || numPathHashes < 0)
            throw new ParserException("Corrupt pak FullDirectoryIndex (flat) detected");

        var numDirAnchors = (numDirs + restartInterval - 1) / restartInterval;

        directoryIndex.Position += numPathHashes * sizeof(ulong); // SortedPathHashes
        directoryIndex.Position += numPathHashes * sizeof(int); // HashLocations
        directoryIndex.Position += (numDirAnchors + 1) * sizeof(int); // DirAnchorOffset

        var dirFileStart = directoryIndex.ReadArray<int>(numDirs + 1);
        var fileNameOffsets = directoryIndex.ReadArray<int>(numFiles + 1);
        var fileLocations = directoryIndex.ReadArray<int>(numFiles);
        var dirBlob = directoryIndex.ReadArray<byte>(dirBlobBytes);
        var fileBlob = directoryIndex.ReadArray<byte>(fileBlobBytes);
        var trimMountSep = MountPoint.Length > 0 && MountPoint[^1] == '/';

        var dirPos = 0;
        var nameBytes = new byte[256];
        for (var dirIndex = 0; dirIndex < numDirs; dirIndex++)
        {
            var sharedLen = BinaryPrimitives.ReadInt32LittleEndian(dirBlob.AsSpan(dirPos));
            dirPos += sizeof(int);
            var suffixLen = BinaryPrimitives.ReadInt32LittleEndian(dirBlob.AsSpan(dirPos));
            dirPos += sizeof(int);
            var nameLen = sharedLen + suffixLen;
            if (nameBytes.Length < nameLen)
            {
                var grown = new byte[Math.Max(nameLen, nameBytes.Length * 2)];
                Array.Copy(nameBytes, grown, sharedLen);
                nameBytes = grown;
            }

            dirBlob.AsSpan(dirPos, suffixLen).CopyTo(nameBytes.AsSpan(sharedLen));
            dirPos += suffixLen;

            var dirSpan = nameBytes.AsSpan(0, nameLen);
            // Mirror ReadIndexUpdated
            var trimDir = trimMountSep && nameLen > 0 && nameBytes[0] == (byte) '/';
            var dir = Encoding.UTF8.GetString(trimDir ? dirSpan[1..] : dirSpan);

            for (var global = dirFileStart[dirIndex]; global < dirFileStart[dirIndex + 1]; global++)
            {
                var location = fileLocations[global];
                if (location == int.MinValue) continue;

                var nameStart = fileNameOffsets[global];
                var fileName = Encoding.UTF8.GetString(fileBlob.AsSpan(nameStart, fileNameOffsets[global + 1] - nameStart));
                var path = string.Concat(MountPoint, dir, fileName);

                FPakEntry entry;
                if (location >= 0)
                {
                    entry = new FPakEntry(this, path, encodedPakEntries, location);
                }
                else
                {
                    var entryIndex = -location - 1;
                    if (entryIndex < 0 || entryIndex >= nonEncodedEntries.Length)
                    {
                        Log.Warning("Invalid nonencoded pak entry with index {Index}, path {Path}", entryIndex, path);
                        continue;
                    }

                    entry = nonEncodedEntries[entryIndex];
                    entry.Path = path;
                }

                if (entry.IsEncrypted) EncryptedFileCount++;
                files[path] = entry;
            }
        }
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
            Ar.ReadFString,
            () => Ar.ReadTMap(
                Ar.ReadFString,
                Ar.Read<int>,
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
