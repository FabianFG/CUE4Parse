using System;
using System.Collections.Generic;
using System.Text;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO;

public class IoContainerMeta
{
    public readonly FIoContainerMetaHeader Header;
    public readonly FIoChunkId[] ChunkIds;
    public readonly uint[] FileEntryIndices;
    public readonly FIoMetaFileIndexEntry[] FileEntries;
    public readonly FIoMetaDirectoryIndexEntry[] DirectoryEntries;
    public readonly FIoMetaStringTableEntry[] StringTableEntries;
    public readonly byte[] StringTable;

    private readonly FByteArchive _archive;

    public IoContainerMeta(FArchive Ar)
    {
        Header = new FIoContainerMetaHeader(Ar);

        var fileCount = (int) Header.FileCount;
        var directoryCount = (int) Header.DirectoryCount;
        var stringCount = (int) Header.StringCount;

        ChunkIds = Ar.ReadArray<FIoChunkId>(fileCount);
        FileEntryIndices = Ar.ReadArray<uint>(fileCount);
        FileEntries = Ar.ReadArray<FIoMetaFileIndexEntry>(fileCount);
        DirectoryEntries = Ar.ReadArray<FIoMetaDirectoryIndexEntry>(directoryCount);
        StringTableEntries = Ar.ReadArray<FIoMetaStringTableEntry>(stringCount);
        StringTable = Ar.ReadArray<byte>((int)(Ar.Length - Ar.Position));

        _archive = new FByteArchive("String Table Archive", StringTable, Ar.Versions);
    }

    public void TryGetFileAndContainerName(FIoChunkId chunkId, out string fileName, out string containerName)
    {
        fileName = string.Empty;
        containerName = string.Empty;

        var index = Array.IndexOf(ChunkIds, chunkId);
        if (index == -1) return;

        TryGetFileAndContainerName(index, out fileName, out containerName);
    }

    private void TryGetFileAndContainerName(int file, out string fileName, out string containerName)
    {
        fileName = string.Empty;
        containerName = string.Empty;

        if (file == 0) return;

        var fileEntry = FileEntries[file];
        var directory = fileEntry.DirectoryEntry;

        if (fileEntry.Name == 0 || directory == 0)
            return;

        List<uint> segments = [fileEntry.Name];
        while (directory != 0)
        {
            var directoryEntry = DirectoryEntries[directory];
            if (directoryEntry.Name == 0) break;
            segments.Add(directoryEntry.Name);
            directory = directoryEntry.ParentEntry;
        }

        for (var seg = segments.Count - 1; seg >= 0; seg--)
        {
            var s = GetString(segments[seg]);
            fileName += s;
            if (seg > 0) fileName += "/";
        }

        containerName = GetString(fileEntry.ContainerName);
    }

    private string GetString(uint stringTableEntryIndex) => stringTableEntryIndex == 0 ? string.Empty : GetString(StringTableEntries[stringTableEntryIndex]);
    private string GetString(FIoMetaStringTableEntry entry) => Encoding.UTF8.GetString(_archive.ReadBytesAt(entry.Offset, (int)entry.Len));
}
