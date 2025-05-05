using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkConversionTable
{
    public uint Scaling { get; }
    public dynamic Size { get; } // uint for legacy versions, ushort for modern versions
    public List<AkRTPCGraphPoint> GraphPoints { get; }

    public AkConversionTable(FArchive ar)
    {
        if (WwiseVersions.WwiseVersion <= 36)
        {
            Scaling = ar.Read<uint>();
            Size = ar.Read<uint>();
        }
        else
        {
            Scaling = ar.Read<byte>();
            Size = ar.Read<ushort>();
        }

        GraphPoints = [];
        for (int i = 0; i < Size; i++)
        {
            GraphPoints.Add(new AkRTPCGraphPoint(ar));
        }
    }
}
