using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects.OnDemand.V2;

public class FOnDemandChunkEntry
{
    public readonly FSHAHash Hash;
    public readonly uint PartitionIndex;
    public readonly uint PartitionOffset;
    public readonly uint RawSize;
    public readonly uint EncodedSize;
    public readonly FOnDemandChunkBlockInfo BlockInfo;

    public FOnDemandChunkEntry(FArchive Ar, EOnDemandTocMinorVersion minorVersion)
    {
        if (minorVersion >= EOnDemandTocMinorVersion.Partitions)
        {
            Hash = new FSHAHash(Ar, 96 >> 3); // FOnDemandChunkHash -> FHash96 -> THash<96>
            PartitionIndex = Ar.Read<uint>();
            PartitionOffset = Ar.Read<uint>();
        }
        else
        {
            Hash = new FSHAHash(Ar);
        }
        RawSize = Ar.Read<uint>();
        EncodedSize = Ar.Read<uint>();
        BlockInfo = new FOnDemandChunkBlockInfo(Ar);
    }
}
