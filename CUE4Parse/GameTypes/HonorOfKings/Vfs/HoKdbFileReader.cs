using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.GameTypes.HonorOfKings.FileProvider.Objects;
using CUE4Parse.GameTypes.HonorOfKings.Vfs.Objects;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.UE4.VirtualFileSystem;
using GenericReader;

namespace CUE4Parse.GameTypes.HonorOfKings.Vfs;

public sealed class HoKdbFileReader : AbstractAesVfsReader
{
    public static readonly ConcurrentDictionary<ulong, string> HashMap = [];
    private static readonly ConcurrentDictionary<ulong, string> _indexFiles = [];
    private readonly IReadOnlyList<HoKdbContainerStream> _containerStreams;
    public readonly IVfsFileProvider Provider;

    private FArchive? Ar;

    public HoKdbFileReader(FileInfo filePath, IVfsFileProvider provider, VersionContainer? versions = null) : base(System.IO.Path.ChangeExtension(filePath.FullName, ".pak"), versions)
    {
        Ar = new FByteArchive(filePath.FullName, File.ReadAllBytes(filePath.FullName), Versions);

        if (Ar.Read<ulong>() != 0x0403020102000001)
            throw new ParserException("Unknown DB file format");

        Length = Ar.Length;
        Provider = provider;
        MountPoint = "";

        var files = filePath.Directory!
            .EnumerateFiles("*.db", SearchOption.TopDirectoryOnly)
            .Where(file => !string.Equals(file.Name, filePath.Name, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // maybe rewrite it to async
        var containerStreams = new HoKdbContainerStream[files.Length];
        Parallel.ForEach(Enumerable.Range(0, files.Length), i =>
        {
            var file = files[i];
            try
            {
                containerStreams[i] = new HoKdbContainerStream(file, Versions);
            }
            catch (Exception e)
            {
                throw new ParserException($"Failed to open container {file.Name}", e);
            }
        });

        Length += containerStreams.Sum(x => x.Length);
        _containerStreams = containerStreams.AsReadOnly();
    }

    public override string MountPoint { get; protected set; }
    public override bool HasDirectoryIndex { get => true; }
    public override FGuid EncryptionKeyGuid { get; } = new();
    public override bool IsEncrypted { get => false; }
    public override long Length { get; set; }

    public static string ReadFileHashes(string indexDirectory, out HashSet<ulong> hashes)
    {
        hashes = [];
        var path = System.IO.Path.Combine(indexDirectory, "IndexEntries.ind");
        if (!File.Exists(path)) return path;

        try
        {
            var Ar = new GenericBufferReader(File.ReadAllBytes(path));
            hashes = Ar.ReadArray<ulong>().ToHashSet();
        }
        catch
        {
            Log.Error("Failed to read file hashes from {0}", path);
        }

        return path;
    }

    public async Task BuildIndex(string indexDirectory, string indexFile)
    {
        if (Ar is null || !Directory.Exists(indexDirectory)) return;
        var hashesFile = ReadFileHashes(indexDirectory, out var indexHashes);

        var entriesOffsets = HoKdbContainerStream.ReadEntriesOffsets(Ar);
        var hashToCompression = new Dictionary<ulong, byte>(entriesOffsets.Length);
        ReadEntriesData(entriesOffsets, hashToCompression);

        Dictionary<ulong, FHoKEntry> unknownFiles = [];
        foreach (var container in _containerStreams)
        {
            foreach (var hash in container.CompressedChunks.Keys)
            {
                byte compression = hashToCompression.GetValueOrDefault(hash, (byte)0);
                if (HashMap.ContainsKey(hash)) continue;
                var entry = new FHoKEntry(this, container, "", hash, compression);
                unknownFiles[hash] = entry;

            }
        }

        var pending = new Queue<(ulong Hash, string BaseDir)>();

        var ngrTreeHash = FHoKFileHash.Compute("/NGR/dirtree.txt", false);
        if (unknownFiles.ContainsKey(ngrTreeHash))
        {
            indexHashes.Add(ngrTreeHash);
            pending.Enqueue((ngrTreeHash, "NGR"));
        }

        var engineTreeHash = FHoKFileHash.Compute("/Engine/dirtree.txt", false);
        if (unknownFiles.ContainsKey(engineTreeHash))
        {
            indexHashes.Add(engineTreeHash);
            pending.Enqueue((engineTreeHash, "Engine"));
        }


        var processed = new HashSet<ulong>();
        while (pending.Count > 0)
        {
            (ulong hash, string baseDir) = pending.Dequeue();
            if (!processed.Add(hash)) continue;
            if (!unknownFiles.TryGetValue(hash, out var entry)) continue;

            var directories = ProcessIndexFileEntries(entry, baseDir);
            foreach (var directory in directories)
            {
                var childTreePath = string.Concat(directory, "/dirtree.txt");
                var childTreeHash = FHoKFileHash.Compute(childTreePath, true);
                HashMap[childTreeHash] = childTreePath;
                if (indexHashes.Add(childTreeHash) && unknownFiles.ContainsKey(childTreeHash))
                {
                    pending.Enqueue((childTreeHash, directory));
                }
            }
        }

        _ = WriteIndexFileAsync(indexFile);

        try
        {
            using var ms = new MemoryStream();
            await using var bw = new BinaryWriter(ms);
            bw.Write(indexHashes.Count);
            bw.Write(MemoryMarshal.Cast<ulong, byte>(indexHashes.ToArray()));
            bw.Flush();
            await File.WriteAllBytesAsync(hashesFile, ms.GetBuffer()).ConfigureAwait(false);
        }
        catch
        {
            Log.Error("Failed to write hashes file {0}", hashesFile);
        }

        Dispose();
    }

    private async Task WriteIndexFileAsync(string indexFile)
    {
        try
        {
            var files = HashMap.Values.ToArray();
            Array.Sort(files);
            await File.WriteAllLinesAsync(indexFile, files).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to write index file {IndexFile}", indexFile);
        }
    }

    private static List<string> ProcessIndexFileEntries(FHoKEntry entry, string baseDir)
    {
        List<string> directories = [];
        var data = entry.Read();
        _indexFiles[entry.Hash] = baseDir + "/dirtree.txt";
        var ind = Array.IndexOf(data, (byte) 0x7c);
        var span = data.AsSpan(ind != -1 ? ind + 1 : 0);
        while (true)
        {
            var pos = span.IndexOf<byte>(0x2c);
            var infoSpan = pos != -1 ? span[..pos] : span;
            var fileInfo = Encoding.UTF8.GetString(infoSpan);
            var parts = fileInfo.Split(':');
            if (parts.Length == 3 && int.TryParse(parts[2], out var type))
            {
                var path = string.Concat(baseDir, "/", parts[0]);
                if (type == 0)
                {
                    HashMap[FHoKFileHash.Compute(path, true)] = path;
                }
                else if (type == 1)
                {
                    directories.Add(path);
                }
            }

            if (pos == -1) break;
            span = span[(pos + 1)..];
        }

        return directories;
    }

    private void ReadIndex(StringComparer pathComparer)
    {
        if (Ar is null) return;
        var entriesOffsets = HoKdbContainerStream.ReadEntriesOffsets(Ar);
        var hashToCompression = new Dictionary<ulong, byte>(entriesOffsets.Length);
        ReadEntriesData(entriesOffsets, hashToCompression);

        var files = new Dictionary<string, GameFile>(entriesOffsets.Length, pathComparer);
        var usedHashes = new HashSet<ulong>(entriesOffsets.Length);
        Dictionary<ulong, FHoKEntry> unknownFiles = [];
        foreach (var container in _containerStreams)
        {
            foreach (var hash in container.CompressedChunks.Keys)
            {
                byte compression = hashToCompression.GetValueOrDefault(hash, (byte)0);
                if (HashMap.TryGetValue(hash, out var path) || _indexFiles.TryGetValue(hash, out path))
                {
                    usedHashes.Add(hash);

                    if (path.EndsWith(".g.ubulk"))
                    {
                        // change only if there is no ubulk, for proper fbytebulkdata extraction
                        var newpath = string.Concat(path.AsSpan()[..^7],"ubulk");
                        if(!HashMap.ContainsKey(FHoKFileHash.Compute(newpath, true)))
                        {
                            path = newpath;
                        }
                    }

#if !DEBUG
                    if (!path.EndsWith("ind", StringComparison.OrdinalIgnoreCase) && !path.EndsWith("dirtree.txt", StringComparison.OrdinalIgnoreCase))
#endif
                    {
                        var entry = new FHoKEntry(this, container, path, hash, compression);
                        files[path] = entry;
                    }
                }
                else
                {
                    var entry = new FHoKEntry(this, container, "", hash, compression);
                    unknownFiles[hash] = entry;
                }
            }
        }

        if (unknownFiles.Count > 0)
            Log.Warning("Found {count} unknown entries", unknownFiles.Count);

        foreach (var hash in unknownFiles.Keys.Except(usedHashes))
        {
            var name = hash.ToString();
            string path = string.Concat("NGR/Content/Unknown/", name + ".unk");
            var entry = unknownFiles[hash];
            entry.Path = path;
            files[path] = entry;
        }

        Files = files;
        Ar.Dispose();
    }

    private void ReadEntriesData(int[] entriesOffsets, Dictionary<ulong, byte> hashToCompression)
    {
        HashSet<int> additionalOffsets = [];
        foreach (var x in entriesOffsets)
        {
            Ar.Position = x;
            if (Ar.Read<int>() != x) continue;
            var next = Ar.Read<int>();
            if (next != -1 && !entriesOffsets.Contains(next)) additionalOffsets.Add(next);
            Ar.Position += 16;
            if (Ar.Read<int>() != 52) continue; // nonstandard entries
            Ar.Position += 44;
            var compflags = Ar.Read<byte>();
            Ar.Position += 7;
            var id = Ar.Read<ulong>();
            hashToCompression[id] = compflags;
        }

        if (additionalOffsets.Count > 0)
            ReadEntriesData(additionalOffsets.ToArray(), hashToCompression);
    }

    public override void Mount(StringComparer pathComparer)
    {
        var watch = new Stopwatch();
        watch.Start();

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

    public override byte[] Extract(VfsEntry entry, FByteBulkDataHeader? header = null)
    {
        throw new NotImplementedException();
    }

    public override void Dispose()
    {
        foreach (var containerStream in _containerStreams)
        {
            containerStream.Dispose();
        }
        Ar?.Dispose();
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
