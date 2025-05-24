using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkStinger
{
    public readonly uint TriggerId;
    public readonly uint SegmentId;
    public readonly uint SyncPlayAt;
    public readonly uint CueFilterHash;
    public readonly int DontRepeatTime;
    public readonly uint NumSegmentLookAhead;

    public AkStinger(FArchive Ar)
    {
        TriggerId = Ar.Read<uint>();
        SegmentId = Ar.Read<uint>();
        SyncPlayAt = Ar.Read<uint>();

        if (WwiseVersions.Version > 62)
        {
            CueFilterHash = Ar.Read<uint>();
        }

        DontRepeatTime = Ar.Read<int>();
        NumSegmentLookAhead = Ar.Read<uint>();
    }

    public static List<AkStinger> ReadMultiple(FArchive Ar)
    {
        var numStingers = Ar.Read<uint>();
        var stingers = new List<AkStinger>((int)numStingers);
        for (int i = 0; i < numStingers; i++)
        {
            stingers.Add(new AkStinger(Ar));
        }

        return stingers;
    }
}
