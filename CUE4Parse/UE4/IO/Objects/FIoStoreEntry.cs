using System.Runtime.CompilerServices;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Assets.Objects;
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

        public FIoStoreEntry(IoStoreReader reader, string path, uint tocEntryIndex) : base(reader, path)
        {
            TocEntryIndex = tocEntryIndex;
            ref var offsetLength = ref reader.TocResource.ChunkOffsetLengths[tocEntryIndex];
            Offset = (long) offsetLength.Offset;
            Size = (long) offsetLength.Length;
        }

        public FIoStoreEntry(IoStoreReader reader, uint tocEntryIndex) : base(reader, "NonIndexed/")
        {
            TocEntryIndex = tocEntryIndex;
            Path += $"0x{ChunkId.ChunkId:X8}.{ChunkId.GetExtension(reader)}";

            ref var offsetLength = ref reader.TocResource.ChunkOffsetLengths[tocEntryIndex];
            Offset = (long) offsetLength.Offset;
            Size = (long) offsetLength.Length;
        }

        public IoStoreReader IoStoreReader
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (IoStoreReader) Vfs;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte[] Read(FByteBulkDataHeader? header = null) => Vfs.Extract(this, header);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override FArchive CreateReader(FByteBulkDataHeader? header = null)
        {
            // Use streaming for full file reads (no bulk header) when available
            if (header == null)
            {
                var stream = Vfs.ExtractStream(this);
                if (stream != null)
                    return new FStreamArchive(Path, stream, Vfs.Versions);
            }
            return new FByteArchive(Path, Read(header), Vfs.Versions);
        }
    }
}
