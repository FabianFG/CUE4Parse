using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects
{
    public class FTocMeta
    {
        public readonly long EpochTimestamp;
        public readonly string BuildVersion;
        public readonly string TargetPlatform;

        public FTocMeta(FArchive Ar)
        {
            EpochTimestamp = Ar.Read<long>();
            BuildVersion = Ar.ReadFString();
            TargetPlatform = Ar.ReadFString();
        }
    }
}
