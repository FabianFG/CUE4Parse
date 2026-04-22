using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.GameTypes.HonorOfKings.FileProvider;
using CUE4Parse.GameTypes.HonorOfKings.Vfs.Objects;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.GameTypes.HonorOfKings.Vfs;

public sealed class HoKdbContainerStream : FRandomAccessFileStreamArchive
{
    public Dictionary<ulong, FHoKCompressedChunk[]> CompressedChunks = [];
    private readonly string _path;
    private readonly ulong _hash;

    public HoKdbContainerStream(FileInfo file, VersionContainer versions) : base(file, versions)
    {
        _path = file.FullName;
        if (Read<ulong>() != 0x0403020102000001)
            throw new ParserException("Unknown DB file format");
        Position = 0;
        _hash = CalculateHash();
        var indexPath = Path.Combine(HoKWDefaultFileProvider.GeneratedIndexFolder, Path.GetFileNameWithoutExtension(file.Name) + ".ind");
        if (File.Exists(indexPath))
        {
            VerifyIndex(indexPath);
        }
        else
        {
            CreateIndex(indexPath);
        }
    }

    private void CreateIndex(string indexPath)
    {
        using var byteAr = new FByteArchive(_path, File.ReadAllBytes(_path));
        var offsets = ReadEntriesOffsets(byteAr);
        var index = new Dictionary<ulong, List<FHoKCompressedChunk>>();
        ReadEntries(byteAr, offsets, index);
        using var ms = new MemoryStream();
        using var Ar = new BinaryWriter(ms);
        Ar.Write(_hash);
        Ar.Write(index.Count);
        foreach (var kvp in index)
        {
            var chunks = kvp.Value.ToArray();
            Array.Sort(chunks, static (x, y) => x.Index.CompareTo(y.Index));
            CompressedChunks[kvp.Key] = chunks;
            Ar.Write(kvp.Key);
            Ar.Write(chunks.Length);
            foreach(var chunk in chunks)
            {
                Ar.Write(chunk.Offset);
                Ar.Write(chunk.CompressedSize);
                Ar.Write(chunk.UncompressedSize);
                Ar.Write(chunk.Padding);
                Ar.Write(chunk.Index);
            }
        }

        Ar.Flush();
        ms.Position = 0;
        using var file = File.Create(indexPath);
        ms.CopyTo(file);
    }

    public static int[] ReadEntriesOffsets(FArchive Ar)
    {
        Ar.Position = 0;
        var header = Ar.Read<FHoKHeader>();
        var tables = FEntriesTables(Ar, header.Entries2.Offset);
        var tables1 = FEntriesTables(Ar, header.Entries3.Offset);

        var offsets = new HashSet<int>();
        foreach (var x in tables.Concat(tables1))
        {
            offsets.Add(x.Offset1);
            offsets.Add(x.Offset2);
            offsets.Add(x.Offset3);
        }

        List<FHoKEntryBlock> blocks = [];
        foreach (var x in offsets)
        {
            Ar.Position = x + 4080;
            var block = Ar.Read<FHoKEntryBlock>();
            // fix offsets for duplicate block
            // idk what is the purpose of duplicate so we read all entries
            block.Offset = x;
            blocks.Add(block);
        }

        offsets = [];
        foreach (var block in blocks)
        {
            if (block.Type is 2) continue; // this contains offset for other blocks
            var size = block.EntryCount;
            // skip ids
            Ar.Position = block.Offset + 2040;
            var entries = Ar.ReadArray<int>(size);
            foreach (var offset in entries)
            {
                offsets.Add(offset);
            }
        }

        var result = offsets.ToArray();
        Array.Sort(result);
        return result;
    }

    private static void ReadEntries(FArchive Ar, int[] offsets, Dictionary<ulong, List<FHoKCompressedChunk>> result)
    {
        HashSet<int> additionalOffsets = [];
        foreach (var x in offsets)
        {
            Ar.Position = x;
            if (Ar.Read<int>() != x) continue;
            var next = Ar.Read<int>();
            if (next != -1 && !offsets.Contains(next)) additionalOffsets.Add(next);
            Ar.Position += 16;
            var compressedSize = Ar.Read<int>() - 4;
            var uncompressedSize = Ar.Read<int>();
            var offset = Ar.Position;

            Ar.Position += compressedSize;
            Ar.Position = Ar.Position.Align(4);
            var id = Ar.Read<ulong>();
            var entry = new FHoKCompressedChunk(offset, compressedSize, uncompressedSize, Ar.Read<int>(), Ar.Read<int>());

            if (result.TryGetValue(id, out var list))
            {
                list.Add(entry);
            }
            else
            {
                result[id] = [entry];
            }
        }
        if (additionalOffsets.Count > 0)
            ReadEntries(Ar, additionalOffsets.ToArray(), result);
    }

    private static FHoKEntriesTable[] FEntriesTables(FArchive Ar, int offset)
    {
        if (offset == -1) return [];
        Ar.Position = offset;
        var max = Ar.Read<int>();
        var count = Ar.Read<int>();
        Ar.Position += 16;
        return Ar.ReadArray<FHoKEntriesTable>(count);
    }

    private void HashFEntriesTables(int offset, ref XxHash64 hasher)
    {
        if (offset == -1) return;
        Position = offset;
        var max = Read<int>();
        var len = 24 + max * Unsafe.SizeOf<FHoKEntriesTable>();
        hasher.Append(ReadBytesAt(offset, len));
    }

    private void VerifyIndex(string indexPath)
    {
        using var Ar = new FByteArchive(indexPath, File.ReadAllBytes(indexPath));
        if (Ar.Read<ulong>() == _hash)
        {
            CompressedChunks = Ar.ReadMap(Ar.Read<ulong>, Ar.ReadArray<FHoKCompressedChunk>);
        }
        else
        {
            Ar.Close();
            CreateIndex(indexPath);
        }
    }

    private ulong CalculateHash()
    {
        const int headerLen = 80;
        var hasher = new XxHash64(0xfadeface);
        if (headerLen < Length)
        {
            var header = Read<FHoKHeader>();
            hasher.Append(ReadBytesAt(0, headerLen));
            if (header.Index.Offset != -1)
            {
                hasher.Append(ReadBytesAt(header.Index.Offset, header.Index.Size));
            }

            HashFEntriesTables(header.Entries2.Offset, ref hasher);
            HashFEntriesTables(header.Entries3.Offset, ref hasher);
        }

        return hasher.GetCurrentHashAsUInt64();
    }
}
