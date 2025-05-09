using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkStinger
{
    public uint TriggerId { get; private set; }
    public uint SegmentId { get; private set; }
    public uint SyncPlayAt { get; private set; }
    public uint CueFilterHash { get; private set; }
    public int DontRepeatTime { get; private set; }
    public uint NumSegmentLookAhead { get; private set; }

    public AkStinger(FArchive Ar)
    {
        TriggerId = Ar.Read<uint>();
        SegmentId = Ar.Read<uint>();
        SyncPlayAt = Ar.Read<uint>();

        if (WwiseVersions.WwiseVersion > 62)
        {
            CueFilterHash = Ar.Read<uint>();
        }

        DontRepeatTime = Ar.Read<int>();
        NumSegmentLookAhead = Ar.Read<uint>();
    }

    public static List<AkStinger> ReadMultiple(FArchive Ar)
    {
        var stingers = new List<AkStinger>();
        var numStingers = Ar.Read<uint>();
        for (int i = 0; i < numStingers; i++)
        {
            var stinger = new AkStinger(Ar);
            stingers.Add(stinger);
        }

        return stingers;
    }
}
