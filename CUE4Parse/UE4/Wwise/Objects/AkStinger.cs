using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkStinger
{
    public uint TriggerId { get; private set; }
    public uint SegmentId { get; private set; }
    public uint SyncPlayAt { get; private set; }
    public uint CueFilterHash { get; private set; }
    public int DontRepeatTime { get; private set; }
    public uint numSegmentLookAhead { get; private set; }

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
        numSegmentLookAhead = Ar.Read<uint>();
    }
}
