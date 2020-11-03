using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Vfs;

namespace CUE4Parse.UE4.IO.Objects
{
    public class FIoStoreEntry : VfsEntry
    {
        public override bool IsEncrypted { get; }

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
        }
        
        public override byte[] Read()
        {
            throw new System.NotImplementedException();
        }

        public override FArchive CreateReader()
        {
            throw new System.NotImplementedException();
        }
    }
}