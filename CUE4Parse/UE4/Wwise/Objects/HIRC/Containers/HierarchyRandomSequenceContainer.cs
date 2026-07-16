using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Enums.Flags;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

public class HierarchyRandomSequenceContainer : AbstractHierarchy
{
    public readonly BaseHierarchy BaseParams;
    public readonly ushort LoopCount;
    public readonly ushort? LoopModMin;
    public readonly ushort? LoopModMax;
    public readonly float? TransitionTime;
    public readonly float? TransitionTimeModMin;
    public readonly float? TransitionTimeModMax;
    public readonly ushort AvoidRepeatCount;
    public readonly EAkTransitionMode TransitionMode;
    public readonly EAkRandomMode RandomMode;
    public readonly EAkContainerMode Mode;
    public readonly EPlayListFlags PlaylistFlags;
    public readonly uint[] ChildIds;
    public readonly AkPlayListItem[] Playlist;

    // CAkRanSeqCntr::SetInitialValues
    public HierarchyRandomSequenceContainer(FWwiseArchive Ar) : base()
    {
        Id = Ar.Read<uint>();
        BaseParams = new BaseHierarchy(Ar);
        LoopCount = Ar.Read<ushort>();

        if (Ar.Version > 72)
        {
            LoopModMin = Ar.Read<ushort>();
            LoopModMax = Ar.Read<ushort>();
        }

        if (Ar.Version <= 38)
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

        if (Ar.Version > 36)
        {
            TransitionMode = Ar.Read<EAkTransitionMode>();
            RandomMode = Ar.Read<EAkRandomMode>();
            Mode = Ar.Read<EAkContainerMode>();
        }

        if (Ar.Version <= 89)
        {
            PlaylistFlags = EPlayListFlags.None;
            if (Ar.ReadBool())
                PlaylistFlags |= EPlayListFlags.IsUsingWeight;
            if (Ar.ReadBool())
                PlaylistFlags |= EPlayListFlags.ResetPlayListAtEachPlay;
            if (Ar.ReadBool())
                PlaylistFlags |= EPlayListFlags.IsRestartBackward;
            if (Ar.ReadBool())
                PlaylistFlags |= EPlayListFlags.IsContinuous;
            if (Ar.ReadBool())
                PlaylistFlags |= EPlayListFlags.IsGlobal;
        }
        else
        {
            PlaylistFlags = Ar.Read<EPlayListFlags>();
        }

        ChildIds = new AkChildren(Ar).ChildIds;
        Playlist = new AkPlayList(Ar).PlaylistItems;
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(BaseParams));
        writer.WriteStartObject();
        BaseParams.WriteJson(writer, serializer);
        writer.WriteEndObject();

        writer.WritePropertyName(nameof(LoopCount));
        writer.WriteValue(LoopCount);

        if (LoopModMin.HasValue)
        {
            writer.WritePropertyName(nameof(LoopModMin));
            writer.WriteValue(LoopModMin);
        }

        if (LoopModMax.HasValue)
        {
            writer.WritePropertyName(nameof(LoopModMax));
            writer.WriteValue(LoopModMax);
        }

        if (TransitionTime.HasValue)
        {
            writer.WritePropertyName(nameof(TransitionTime));
            writer.WriteValue(TransitionTime);
        }

        if (TransitionTimeModMin.HasValue)
        {
            writer.WritePropertyName(nameof(TransitionTimeModMin));
            writer.WriteValue(TransitionTimeModMin);
        }

        if (TransitionTimeModMax.HasValue)
        {
            writer.WritePropertyName(nameof(TransitionTimeModMax));
            writer.WriteValue(TransitionTimeModMax);
        }

        writer.WritePropertyName(nameof(AvoidRepeatCount));
        writer.WriteValue(AvoidRepeatCount);

        writer.WritePropertyName(nameof(TransitionMode));
        writer.WriteValue(TransitionMode.ToString());

        writer.WritePropertyName(nameof(RandomMode));
        writer.WriteValue(RandomMode.ToString());

        writer.WritePropertyName(nameof(Mode));
        writer.WriteValue(Mode.ToString());

        writer.WritePropertyName(nameof(PlaylistFlags));
        writer.WriteValue(PlaylistFlags.ToString());

        writer.WritePropertyName(nameof(ChildIds));
        serializer.Serialize(writer, ChildIds);

        writer.WritePropertyName(nameof(Playlist));
        serializer.Serialize(writer, Playlist);

        writer.WriteEndObject();
    }
}
