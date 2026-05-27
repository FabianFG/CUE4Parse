using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects.OnDemand.V2;

public class FOnDemandChunkEntry
{
    public FSHAHash Hash;
    public uint RawSize;
    public uint EncodedSize;
    public FOnDemandChunkBlockInfo BlockInfo;

    public FOnDemandChunkEntry(FArchive Ar)
    {
        Hash = new FSHAHash(Ar);
        RawSize = Ar.Read<uint>();
        EncodedSize = Ar.Read<uint>();
        BlockInfo = new FOnDemandChunkBlockInfo(Ar);
    }
}