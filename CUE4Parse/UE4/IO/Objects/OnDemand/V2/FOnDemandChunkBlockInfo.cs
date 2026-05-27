using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects.OnDemand.V2;

public class FOnDemandChunkBlockInfo
{
    public uint OffsetOrSize;
    public bool bHasOffset;
    public uint CountOrHash;
    
    public FOnDemandChunkBlockInfo(FArchive Ar)
    {
        var offsetOrSize = Ar.Read<uint>();
        
        OffsetOrSize = offsetOrSize & 0x7FFFFFFFu;
        bHasOffset = (offsetOrSize >> 31) != 0;
        CountOrHash = Ar.Read<uint>();
    }
}