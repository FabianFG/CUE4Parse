using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.VirtualFileCache
{
    public class FDataReference
    {
        public readonly FRangeId[] Ranges;
        public readonly long LastReferencedUnixTime;
        public readonly uint TotalSize;

        public FDataReference(FByteArchive Ar)
        {
            Ranges = Ar.ReadArray<FRangeId>();
            LastReferencedUnixTime = Ar.Read<long>();
            TotalSize = Ar.Read<uint>();
        }

        public void Touch()
        {

        }
    }
}
