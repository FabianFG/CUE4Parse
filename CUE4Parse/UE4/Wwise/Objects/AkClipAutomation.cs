using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkClipAutomation
{
    public readonly uint UClipIndex;
    public readonly uint EAutoType;
    public readonly List<AkRtpcGraphPoint> GraphPoints;

    public AkClipAutomation(FArchive Ar)
    {
        UClipIndex = Ar.Read<uint>();
        EAutoType = Ar.Read<uint>();

        var numPoints = Ar.Read<uint>();
        GraphPoints = new List<AkRtpcGraphPoint>((int) numPoints);
        for (int i = 0; i < numPoints; i++)
        {
            GraphPoints.Add(new AkRtpcGraphPoint(Ar));
        }
    }
}
