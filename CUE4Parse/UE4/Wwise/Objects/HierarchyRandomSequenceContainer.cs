using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects;

public class HierarchyRandomSequenceContainer : BaseHierarchyContainer
{
    public ushort LoopCount { get; private set; }
    public ushort? LoopModMin { get; private set; }
    public ushort? LoopModMax { get; private set; }
    public float? TransitionTime { get; private set; }
    public float? TransitionTimeModMin { get; private set; }
    public float? TransitionTimeModMax { get; private set; }
    public ushort AvoidRepeatCount { get; private set; }
    public byte TransitionMode { get; private set; }
    public byte RandomMode { get; private set; }
    public byte Mode { get; private set; }
    public new byte ByBitVector { get; private set; }
    public uint[] ChildIDs { get; private set; }
    public uint[] PlaylistItems { get; private set; }

    public HierarchyRandomSequenceContainer(FArchive Ar) : base(Ar)
    {
        LoopCount = Ar.Read<ushort>();

        if (WwiseVersions.WwiseVersion > 72)
        {
            LoopModMin = Ar.Read<ushort>();
            LoopModMax = Ar.Read<ushort>();
        }

        if (WwiseVersions.WwiseVersion <= 38)
        {
            TransitionTime = Ar.Read<int>();
            TransitionTimeModMin = Ar.Read<int>();
            TransitionTimeModMax = Ar.Read<int>();
        }
        else
        {
            TransitionTime = Ar.Read<float>();
            TransitionTimeModMin = Ar.Read<float>();
            TransitionTimeModMax = Ar.Read<float>();
        }

        AvoidRepeatCount = Ar.Read<ushort>();

        if (WwiseVersions.WwiseVersion > 36)
        {
            TransitionMode = Ar.Read<byte>();
            RandomMode = Ar.Read<byte>();
            Mode = Ar.Read<byte>();
        }

        if (WwiseVersions.WwiseVersion > 89)
        {
            ByBitVector = Ar.Read<byte>();
        }

        ChildIDs = new AkChildren(Ar).ChildIDs;
        PlaylistItems = ReadPlaylist(Ar);
    }

    private uint[] ReadPlaylist(FArchive Ar)
    {
        var itemCount = WwiseVersions.WwiseVersion > 38 ? Ar.Read<ushort>() : Ar.Read<uint>();
        var items = new uint[itemCount];
        for (int i = 0; i < itemCount; i++)
        {
            items[i] = Ar.Read<uint>();
        }
        return items;
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WritePropertyName("LoopCount");
        writer.WriteValue(LoopCount);

        if (LoopModMin.HasValue)
        {
            writer.WritePropertyName("LoopModMin");
            writer.WriteValue(LoopModMin);
        }

        if (LoopModMax.HasValue)
        {
            writer.WritePropertyName("LoopModMax");
            writer.WriteValue(LoopModMax);
        }

        if (TransitionTime.HasValue)
        {
            writer.WritePropertyName("TransitionTime");
            writer.WriteValue(TransitionTime);
        }

        if (TransitionTimeModMin.HasValue)
        {
            writer.WritePropertyName("TransitionTimeModMin");
            writer.WriteValue(TransitionTimeModMin);
        }

        if (TransitionTimeModMax.HasValue)
        {
            writer.WritePropertyName("TransitionTimeModMax");
            writer.WriteValue(TransitionTimeModMax);
        }

        writer.WritePropertyName("AvoidRepeatCount");
        writer.WriteValue(AvoidRepeatCount);

        writer.WritePropertyName("TransitionMode");
        writer.WriteValue(TransitionMode);

        writer.WritePropertyName("RandomMode");
        writer.WriteValue(RandomMode);

        writer.WritePropertyName("Mode");
        writer.WriteValue(Mode);

        writer.WritePropertyName("ByBitVector");
        writer.WriteValue(ByBitVector);

        writer.WritePropertyName("ChildIDs");
        serializer.Serialize(writer, ChildIDs);

        writer.WritePropertyName("PlaylistItems");
        serializer.Serialize(writer, PlaylistItems);

        writer.WriteEndObject();
    }
}
