using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchyRandomSequenceContainer : BaseHierarchy
{
    public readonly ushort LoopCount;
    public readonly ushort? LoopModMin;
    public readonly ushort? LoopModMax;
    public readonly float? TransitionTime;
    public readonly float? TransitionTimeModMin;
    public readonly float? TransitionTimeModMax;
    public readonly ushort AvoidRepeatCount;
    public readonly ETransitionMode TransitionMode;
    public readonly ERandomMode RandomMode;
    public readonly ERandomSequenceContainerMode Mode;
    public readonly EPlayListFlags PlaylistFlags;
    public readonly uint[] ChildIds;
    public readonly List<AkPlayList.AkPlayListItem> Playlist;

    public HierarchyRandomSequenceContainer(FArchive Ar) : base(Ar)
    {
        LoopCount = Ar.Read<ushort>();

        if (WwiseVersions.Version > 72)
        {
            LoopModMin = Ar.Read<ushort>();
            LoopModMax = Ar.Read<ushort>();
        }

        if (WwiseVersions.Version <= 38)
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

        if (WwiseVersions.Version > 36)
        {
            TransitionMode = Ar.Read<ETransitionMode>();
            RandomMode = Ar.Read<ERandomMode>();
            Mode = Ar.Read<ERandomSequenceContainerMode>();
        }

        if (WwiseVersions.Version > 89)
        {
            PlaylistFlags = Ar.Read<EPlayListFlags>();
        }

        ChildIds = new AkChildren(Ar).ChildIds;
        Playlist = new AkPlayList(Ar).PlaylistItems;
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
        writer.WriteValue(TransitionMode.ToString());

        writer.WritePropertyName("RandomMode");
        writer.WriteValue(RandomMode.ToString());

        writer.WritePropertyName("Mode");
        writer.WriteValue(Mode.ToString());

        writer.WritePropertyName("PlaylistFlags");
        writer.WriteValue(PlaylistFlags.ToString());

        writer.WritePropertyName("ChildIds");
        serializer.Serialize(writer, ChildIds);

        writer.WritePropertyName("Playlist");
        serializer.Serialize(writer, Playlist);

        writer.WriteEndObject();
    }
}
