using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkClipAutomation
{
    public uint UClipIndex { get; }
    public uint EAutoType { get; }
    public uint UNumPoints { get; }
    public List<AkRTPCGraphPoint> GraphPoints { get; }

    public AkClipAutomation(FArchive Ar)
    {
        UClipIndex = Ar.Read<uint>();
        EAutoType = Ar.Read<uint>();
        UNumPoints = Ar.Read<uint>();

        GraphPoints = new List<AkRTPCGraphPoint>((int) UNumPoints);
        for (int i = 0; i < UNumPoints; i++)
        {
            GraphPoints.Add(new AkRTPCGraphPoint(Ar));
        }
    }
}
