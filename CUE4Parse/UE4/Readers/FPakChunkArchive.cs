using System;
using CUE4Parse.UE4.Pak.Objects;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Readers;

public class FPakChunkArchive : FChunkedArchive {
    public FPakChunkArchive(string path, FPakEntry entry, VersionContainer versions) : base(path, versions) {
        Entry = entry;
    }

    public override long Length => Entry.UncompressedSize;
    public FPakEntry Entry { get; }
    public override object Clone() {
        var instance = new FPakChunkArchive(Name, Entry, Versions) {
            Position = Position,
        };
        instance.ImportBuffer(this);
        return instance;
    }

    protected override Span<byte> ReadChunks(long offset, long size) {
        var remaining = Math.Min(size, Entry.UncompressedSize - offset);
        return remaining <= 0 ? Span<byte>.Empty : Entry.PakFileReader.Read(Entry, offset, remaining);
    }
}
