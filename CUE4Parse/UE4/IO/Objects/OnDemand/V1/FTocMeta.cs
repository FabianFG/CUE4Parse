using System;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.IO.Objects.OnDemand.V1;

public class FTocMeta
{
    public DateTime EpochTimeStamp;
    public string BuildVersion;
    public string TargetVersion;

    public FTocMeta(FArchive Ar)
    {
        EpochTimeStamp = DateTimeOffset.FromUnixTimeSeconds(Ar.Read<long>()).UtcDateTime;
        BuildVersion = Ar.ReadFString();
        TargetVersion = Ar.ReadFString();
    }
}
