using System.Runtime.CompilerServices;
using CUE4Parse.Compression;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Vfs;

namespace CUE4Parse.UE4.IO.Objects
{
    public class FIoStoreEntry : VfsEntry
    {
        public override bool IsEncrypted { get; }
        public override CompressionMethod CompressionMethod { get; }

        public readonly uint UserData;
        public readonly FIoChunkId ChunkId;

        public FIoStoreEntry(IoStoreReader reader, string path, uint userData) : base(reader)
        {
            Path = path;
            UserData = userData;
            ChunkId = reader.TocResource.ChunkIds[userData];
            ref var offsetLength = ref reader.TocResource.ChunkOffsetLengths[userData];
            Offset = (long) offsetLength.Offset;
            Size = (long) offsetLength.Length;
            IsEncrypted = reader.IsEncrypted;
            CompressionMethod =
                reader.TocResource.CompressionMethods[
                    reader.TocResource.CompressionBlocks[userData].CompressionMethodIndex];
        }

        public override byte[] Read() => Vfs.Extract(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override FArchive CreateReader() => new FByteArchive(Path, Read(), Versions);
    }
}