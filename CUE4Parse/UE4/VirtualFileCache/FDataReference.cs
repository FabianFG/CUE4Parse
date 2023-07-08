using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.VirtualFileCache;

public class FDataReference(FArchive Ar)
{
    public readonly FRangeId[] Ranges = Ar.ReadArray<FRangeId>();
    public readonly long LastReferencedUnixTime = Ar.Read<long>();
    public readonly uint TotalSize = Ar.Read<uint>();

    public void Touch()
    {

    }
}
