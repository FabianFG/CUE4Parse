using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects
{
    public enum FIoStoreTocEntryMetaFlags : byte
    {
        None,
        Compressed		= (1 << 0),
        MemoryMapped	= (1 << 1)
    }
    
    public readonly struct FIoStoreTocEntryMeta
    {
        public readonly FIoChunkHash ChunkHash;
        public readonly FIoStoreTocEntryMetaFlags Flags;

        public FIoStoreTocEntryMeta(FArchive Ar)
        {
            ChunkHash = new FIoChunkHash(Ar);
            Flags = Ar.Read<FIoStoreTocEntryMetaFlags>();
        }
    }
}