using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Wwise.Objects;

public class AkMusicMarkerWwise
{
    public uint Id { get; private set; }
    public double Position { get; private set; }
    public string? MarkerName { get; private set; }

    public AkMusicMarkerWwise(FArchive Ar)
    {
        Id = Ar.Read<uint>();

        Position = Ar.Read<double>();

        if (WwiseVersions.WwiseVersion <= 62)
        {
            // No additional fields for version <= 62
        }
        else if (WwiseVersions.WwiseVersion <= 136)
        {
            var stringSize = Ar.Read<uint>();
            if (stringSize > 0)
            {
                MarkerName = Ar.ReadString();
            }
        }
        else
        {
            MarkerName = Ar.ReadString();
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
