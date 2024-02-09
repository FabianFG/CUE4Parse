using System;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Readers;

public class FIoChunkArchive : FChunkedArchive {
    public FIoChunkArchive(string path, FIoStoreEntry entry, VersionContainer versions) : base(path, versions) {
        Entry = entry;
    }

    public override long Length => Entry.Size;
    public FIoStoreEntry Entry { get; }
    public override object Clone() {
        var instance = new FIoChunkArchive(Name, Entry, Versions) {
            Position = Position,
        };
        instance.ImportBuffer(this);
        return instance;
    }

    protected override Span<byte> ReadChunks(long offset, long size) {
        var remaining = Math.Min(size, Entry.Size - offset);
        return remaining <= 0 ? Span<byte>.Empty : Entry.IoStoreReader.Read(offset + Entry.Offset, remaining);
    }
}
