using System.Runtime.CompilerServices;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.VirtualFileSystem;

namespace CUE4Parse.UE4.IO.Objects
{
    public class FIoStoreEntry : VfsEntry
    {
        public override bool IsEncrypted => IoStoreReader.IsEncrypted;
        public override CompressionMethod CompressionMethod
        {
            get
            {
                var tocResource = IoStoreReader.TocResource;
                var firstBlockIndex = (int) (Offset / tocResource.Header.CompressionBlockSize);
                return tocResource.CompressionMethods[tocResource.CompressionBlocks[firstBlockIndex].CompressionMethodIndex];
            }
        }

        public readonly uint TocEntryIndex;
        public FIoChunkId ChunkId => IoStoreReader.TocResource.ChunkIds[TocEntryIndex];

        public FIoStoreEntry(IoStoreReader reader, string path, uint tocEntryIndex) : base(reader)
        {
            Path = path;
            TocEntryIndex = tocEntryIndex;
            ref var offsetLength = ref reader.TocResource.ChunkOffsetLengths[tocEntryIndex];
            Offset = (long) offsetLength.Offset;
            Size = (long) offsetLength.Length;
        }

        public IoStoreReader IoStoreReader
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (IoStoreReader) Vfs;
        }

        public override byte[] Read() => Vfs.Extract(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override FArchive CreateReader() => new FByteArchive(Path, Read(), Vfs.Versions);
    }
}
