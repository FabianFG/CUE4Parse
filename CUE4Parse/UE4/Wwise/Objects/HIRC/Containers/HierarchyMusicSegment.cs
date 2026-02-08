using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

// CAkMusicSegment
public class HierarchyMusicSegment : BaseHierarchyMusic
{
    public readonly AkMeterInfo MeterInfo;
    public readonly AkStinger[] Stingers;
    public readonly double Duration;
    public readonly AkMusicMarkerWwise[] Markers;

    // CAkMusicSegment::SetInitialValues
    public HierarchyMusicSegment(FArchive Ar) : base(Ar)
    {
        MeterInfo = new AkMeterInfo(Ar);
        Stingers = AkStinger.ReadArray(Ar);
        Duration = Ar.Read<double>();
        Markers = AkMusicMarkerWwise.ReadArray(Ar);
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(MeterInfo));
        serializer.Serialize(writer, MeterInfo);

        writer.WritePropertyName(nameof(Duration));
        writer.WriteValue(Duration);

        writer.WritePropertyName(nameof(Markers));
        serializer.Serialize(writer, Markers);

        writer.WriteEndObject();
    }
}
