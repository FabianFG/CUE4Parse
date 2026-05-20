using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects.OnDemand.V1;

public class FOnDemandTocEntry
{
    public FSHAHash Hash;
    public FIoChunkId ChunkId;
    public ulong RawSize;
    public ulong EncodedSize;
    public uint BlockOffset;
    public uint BlockCount;
    
    public FOnDemandTocEntry(FArchive Ar)
    {
        Hash = new FSHAHash(Ar);
        ChunkId = Ar.Read<FIoChunkId>();
        RawSize = Ar.Read<ulong>();
        EncodedSize = Ar.Read<ulong>();
        BlockOffset = Ar.Read<uint>();
        BlockCount  = Ar.Read<uint>();
    }
}