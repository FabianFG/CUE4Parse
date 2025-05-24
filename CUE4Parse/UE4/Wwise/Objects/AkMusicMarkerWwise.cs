using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkMusicMarkerWwise
{
    public readonly uint Id;
    public readonly double Position;
    public readonly string? MarkerName;

    public AkMusicMarkerWwise(FArchive Ar)
    {
        Id = Ar.Read<uint>();

        Position = Ar.Read<double>();

        if (WwiseVersions.Version <= 62)
        {
            // No additional fields for version <= 62
        }
        else if (WwiseVersions.Version <= 136)
        {
            var stringSize = Ar.Read<uint>();
            if (stringSize > 0)
            {
                MarkerName = Ar.ReadString();
            }
        }
        else
        {
            MarkerName = WwiseReader.ReadStzString(Ar);
        }
    }

    public static List<AkMusicMarkerWwise> ReadMultiple(FArchive Ar)
    {
        var markers = new List<AkMusicMarkerWwise>();
        var numMarkers = Ar.Read<uint>();
        for (int i = 0; i < numMarkers; i++)
        {
            var marker = new AkMusicMarkerWwise(Ar);
            markers.Add(marker);
        }

        return markers;
    }
}
