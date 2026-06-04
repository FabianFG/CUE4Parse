using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects.OnDemand.V2;

public class FOnDemandPartitionEntry
{
    public FSHAHash Hash;
    public uint Size;

    public FOnDemandPartitionEntry(FArchive Ar)
    {
        Hash = new FSHAHash(Ar);
        Size = Ar.Read<uint>();
    }
}