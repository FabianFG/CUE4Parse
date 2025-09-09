using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkConversionTable
{
    public readonly uint Scaling;
    public readonly dynamic Size; // uint for legacy versions, ushort for modern versions
    public readonly List<AkRtpcGraphPoint> GraphPoints;

    public AkConversionTable(FArchive Ar)
    {
        if (WwiseVersions.Version <= 36)
        {
            Scaling = Ar.Read<uint>();
            Size = Ar.Read<uint>();
        }
        else
        {
            Scaling = Ar.Read<byte>();
            Size = Ar.Read<ushort>();
        }

        GraphPoints = [];
        for (int i = 0; i < Size; i++)
        {
            GraphPoints.Add(new AkRtpcGraphPoint(Ar));
        }
    }
}
