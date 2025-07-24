using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using CUE4Parse.Compression;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using CUE4Parse.Utils;
using GenericReader;
using static CUE4Parse.Compression.Compression;

namespace CUE4Parse.GameTypes.AshEchoes.FileProvider;

public class AEDefaultFileProvider : DefaultFileProvider
{
    public AEDefaultFileProvider(string directory, SearchOption searchOption, VersionContainer? versions = null,
        StringComparer? pathComparer = null) : base(directory, searchOption, versions, pathComparer)
    {
    }

    public override void Initialize()
    {
        if (!_workingDirectory.Exists)
            throw new DirectoryNotFoundException("The game directory could not be found.");

        var availableFiles = new List<Dictionary<string, GameFile>> {IterateFiles(_workingDirectory, _searchOption)};

        foreach (var osFiles in availableFiles)
        {
            Files.AddFiles(osFiles);
        }
    }

    public override int LoadVirtualPaths(FPackageFileVersion version, CancellationToken cancellationToken = default)
    {
        base.LoadVirtualPaths(version, cancellationToken);
        VirtualPaths["Paper2D"] = "Engine/Plugins/2D/Paper2D";
        VirtualPaths["Composure"] = "Engine/Plugins/Compositing/Composure";
        VirtualPaths["SpeedTreeImporter"] = "Engine/Plugins/Editor/SpeedTreeImporter";
        VirtualPaths["MovieRenderPipeline"] = "Engine/Plugins/MovieScene/MovieRenderPipeline";
        return VirtualPaths.Count;
    }

    protected Dictionary<string, GameFile> IterateFiles(DirectoryInfo directory, SearchOption option)
    {
        var osFiles = new Dictionary<string, GameFile>(PathComparer);
        if (!directory.Exists) return osFiles;

        var mountPoint = directory.Name + '/';
        string indexPath = "";
        foreach (var file in directory.EnumerateFiles("*.*", option))
        {
            var upperExt = file.Extension.SubstringAfter('.').ToUpper();

            // Only load containers if .uproject file is not found
            if (upperExt is "PAK")
            {
                if (file.Directory.FullName.EndsWith("Content\\Paks", StringComparison.OrdinalIgnoreCase))
                {
                    string? contentPath = file.Directory.Parent?.Parent?.FullName;
                    var storePath = Path.Combine(contentPath ?? "", "Store");
                    var indexpath = Path.Combine(storePath, "new_index");
                    if (File.Exists(indexpath))
                    {
                        indexPath = indexpath;
                    }
                }
                RegisterVfs(file);
                continue;
            }

            // Register local file only if it has a known extension, we don't need every file
            if (!GameFile.UeKnownExtensions.Contains(upperExt, StringComparer.OrdinalIgnoreCase))
                continue;

            var osFile = new OsGameFile(_workingDirectory, file, mountPoint, Versions);
            osFiles[osFile.Path] = osFile;
        }

        if (string.IsNullOrEmpty(indexPath))
        {
            Log.Warning("Index file not found, using only ordinary pak files.");
            return osFiles;
        }

        using var indexAr = new GenericBufferReader(File.ReadAllBytes(indexPath));
        var header = indexAr.Read<FAEFileHeader>();

        var chunksSizes = indexAr.ReadArray<uint>(header.ChunksCount);
        var total = new byte[header.Size];

        var offset = 0;
        for (int i = 1; i <= chunksSizes.Length; i++)
        {
            var compressedData = indexAr.ReadArray<byte>((int)chunksSizes[i - 1]);
            long uncompressedSize = i == chunksSizes.Length ? header.Size - header.ChunkSize * (i - 1) : header.ChunkSize;
            Decompress( compressedData,0, compressedData.Length, total,  offset, (int)uncompressedSize, header.Compression);
            offset += (int) uncompressedSize;
        }

        using var Ar = new GenericBufferReader(total);
        var fileEntries = new List<FFileEntry>();
        while (Ar.Position < Ar.Length)
        {
            var entry = new FFileEntry(Ar);
            fileEntries.Add(entry);
        }

        var repoPath = Path.Combine(Directory.GetParent(indexPath).FullName, "repo6");
        foreach (var entry in fileEntries)
        {
            if (string.IsNullOrEmpty(entry.Name) || entry.Size <= 0)
                continue;

            var path = Path.Combine(repoPath, entry.Folder, entry.FileName);
            if (!File.Exists(path))
            {
                Log.Warning($"File not found: {path}");
                continue;
            }

            var reader = new AEPakFileReader(path, entry.Name, Versions);
            PostLoadReader(reader);
        }

        return osFiles;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct FAEFileHeader
{
    public long Size;
    public long Flags;
    public long FileSize;
    public uint ChunkSize;
    public ushort Comp;
    public ushort Map;

    public int ChunksCount => (int)((Size + ChunkSize - 1) / ChunkSize);

    public CompressionMethod Compression => Comp switch
    {
        0 => CompressionMethod.None,
        1 => CompressionMethod.LZ4,
        4 => CompressionMethod.Oodle,
        _ => throw new NotSupportedException($"Unsupported compression type: {Comp}")
    };
}

public class FAEPakEntry : FPakEntry
{
    public new readonly long CompressedSize;
    public new readonly long UncompressedSize;
    public override bool IsEncrypted { get => false; }
    public override CompressionMethod CompressionMethod { get; }
    public new readonly FPakCompressedBlock[] CompressionBlocks = [];
    public new readonly uint CompressionBlockSize;

    public new readonly int StructSize;
    public new bool IsCompressed => UncompressedSize != CompressedSize && CompressionBlockSize > 0;

    public FAEPakEntry(AEPakFileReader vfs, string path, bool singleFile, long size = 0) : base(vfs, path, size)
    {
        var Ar = vfs.Ar;
        var entryStart = Ar.Position;
        var header = Ar.Read<FAEFileHeader>();
        Size = header.Size;
        UncompressedSize = header.Size;
        Offset = Ar.Position;
        CompressionMethod = header.Compression;
        if (header.Comp == 0)
        {
            CompressedSize = header.Size;
        }
        else
        {
            CompressionBlockSize = header.ChunkSize;
            CompressionBlocks = new FPakCompressedBlock[header.ChunksCount];
            var chunkSizes = Ar.ReadArray<uint>(header.ChunksCount);
            Offset = Ar.Position;
            var start = Offset;
            for (int i = 0; i < header.ChunksCount; i++)
            {
                CompressionBlocks[i] = new FPakCompressedBlock(start, start + chunkSizes[i]);
                start += chunkSizes[i];
            }
            CompressedSize = start - Offset;
        }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte[] Read() => Vfs.Extract(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override FArchive CreateReader() => new FByteArchive(Path, Read(), Vfs.Versions);
}

public class AEPakFileReader : AbstractAesVfsReader
{
    private readonly string _indexName;
    public readonly FArchive Ar;

    public AEPakFileReader(string filePath, string name, VersionContainer? versions = null) : base(filePath + ".pak", versions)
    {
        Ar = new FRandomAccessFileStreamArchive(filePath, Versions);
        Length = Ar.Length;
        _indexName = name;
    }

    public override string MountPoint { get; protected set; }
    public override bool HasDirectoryIndex { get => true; }
    public override FGuid EncryptionKeyGuid { get; } = new();
    public override bool IsEncrypted { get => false; }
    public override long Length { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    string FixFilePath(string path)
    {
        return path.StartsWith("Engine", StringComparison.OrdinalIgnoreCase) ? path : string.Concat("GateUS/", path);
    }

    public void ReadIndex(StringComparer pathComparer)
    {
        if (!_indexName.EndsWith('$'))
        {
            var name = FixFilePath(_indexName);
            var entry = new FAEPakEntry(this, name, true);
            var files = new Dictionary<string, GameFile>(1, pathComparer);
            files[name] = entry;
            Files = files;
        }
        else
        {
            Ar.Position = Length - 8;
            var indexLength = Ar.Read<uint>();
            Ar.Position = Length - 12 - indexLength;
            var indexOffset = Ar.Position;
            var indexHash = Ar.Read<uint>();
            var entries = new List<FAssetEntry>(4);
            while (Ar.Position < indexOffset + indexLength + 4)
            {
                var entry = new FAssetEntry(Ar);
                entries.Add(entry);
            }

            var files = new Dictionary<string, GameFile>(entries.Count, pathComparer);
            foreach (var entry in entries)
            {
                var name = FixFilePath(entry.Name);
                Ar.Position = indexOffset - entry.Offset;
                var file = new FAEPakEntry(this, name, false);
                files[name] = file;
            }

            Files = files;
        }
    }

    public override void Mount(StringComparer pathComparer)
    {
        var watch = new Stopwatch();
        watch.Start();
        MountPoint = "/" + _indexName.SubstringBeforeLast("/");

        ReadIndex(pathComparer);

        if (Globals.LogVfsMounts)
        {
            var elapsed = watch.Elapsed;
            var sb = new StringBuilder($"Pak \"{Name}\": {FileCount} files");
            if (MountPoint.Contains("/"))
                sb.Append($", mount point: \"{MountPoint}\"");
            sb.Append($", order {ReadOrder}");
            sb.Append($", in {elapsed}");
            Log.Information(sb.ToString());
        }
    }

    public override byte[] Extract(VfsEntry entry)
    {
        if (entry is not FAEPakEntry pakEntry || entry.Vfs != this) throw new ArgumentException($"Wrong pak file reader, required {entry.Vfs.Name}, this is {Name}");
        // If this reader is used as a concurrent reader create a clone of the main reader to provide thread safety
        var reader = IsConcurrent ? (FArchive) Ar.Clone() : Ar;
        if (pakEntry.IsCompressed)
        {
            var uncompressed = new byte[(int) pakEntry.UncompressedSize];
            var uncompressedOff = 0;
            foreach (var block in pakEntry.CompressionBlocks)
            {
                var blockSize = (int) block.Size;
                var compressed = ReadAndDecryptAt(block.CompressedStart, blockSize, reader, pakEntry.IsEncrypted);
                var uncompressedSize = BitConverter.ToInt32(compressed, 0);
                Decompress(compressed, 4, blockSize - 4, uncompressed, uncompressedOff, uncompressedSize, pakEntry.CompressionMethod);
                uncompressedOff += uncompressedSize;
            }

            return uncompressed;
        }

        var size = (int) pakEntry.UncompressedSize;
        var data = ReadAndDecryptAt(pakEntry.Offset, size, reader, pakEntry.IsEncrypted);
        return data;
    }

    public override void Dispose()
    {
        Ar.Dispose();
    }

    public override byte[] MountPointCheckBytes()
    {
        throw new NotImplementedException();
    }

    protected override byte[] ReadAndDecrypt(int length)
    {
        throw new NotImplementedException();
    }
}

public class FAssetEntry
{
    public FGuid Guid;
    public string Name;
    public uint Offset;
    public uint Size;
    public uint Hash;
    public byte[] data;

    public FAssetEntry(GenericBufferReader Ar)
    {
        Guid = Ar.Read<FGuid>();
        Offset = Ar.Read<uint>();
        Size = Ar.Read<uint>();
        Name = Encoding.UTF8.GetString(Ar.ReadArray<byte>(Ar.Read<ushort>()));
        Hash = Ar.Read<uint>();
    }

    public FAssetEntry(FArchive Ar)
    {
        Guid = Ar.Read<FGuid>();
        Offset = Ar.Read<uint>();
        Size = Ar.Read<uint>();
        Name = Encoding.UTF8.GetString(Ar.ReadArray<byte>(Ar.Read<ushort>()));
        Hash = Ar.Read<uint>();
    }
}


public class FFileEntry
{
    public string Folder;
    public string FileName;
    public string Name;
    public long Size;

    public FFileEntry(GenericBufferReader Ar)
    {
        Folder = Encoding.UTF8.GetString(Ar.ReadArray<byte>(2));
        FileName = Encoding.UTF8.GetString(Ar.ReadArray<byte>(38));
        Ar.Position += 1;//separator
        var namebytes = new List<byte>(64);
        while (true)
        {
            byte b = Ar.Read<byte>();
            if (b == 9) break; // null terminator
            namebytes.Add(b);
        }

        Name = Encoding.UTF8.GetString(namebytes.ToArray());
        var sizebytes = new List<byte>(8);
        while (Ar.Position < Ar.Length)
        {
            byte b = Ar.Read<byte>();
            if (b == 10) break; // null terminator
            sizebytes.Add(b);
        }
        Size = long.TryParse(Encoding.UTF8.GetString(sizebytes.ToArray()), out long size) ? size : 0;
    }
}
