using System.Collections.Generic;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Objects.Actions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchyEventAction : AbstractHierarchy
{
    public readonly EEventActionScope EventActionScope;
    public readonly EEventActionType EventActionType;
    public readonly uint ReferencedId;
    public List<AkProp> Props { get; protected set; }
    public List<AkPropRange> PropRanges { get; protected set; }

    public object? ActionData { get; private set; }

    public HierarchyEventAction(FArchive Ar) : base(Ar)
    {
        EventActionScope = Ar.Read<EEventActionScope>();
        EventActionType = Ar.Read<EEventActionType>();

        ReferencedId = Ar.Read<uint>();

        var isBus = Ar.Read<byte>();
        AkPropBundle propBundle = new(Ar);
        Props = propBundle.Props;
        PropRanges = propBundle.PropRanges;

        ActionData = EventActionType switch
        {
            EEventActionType.Play => new AkActionPlay(Ar),
            EEventActionType.Stop => new AkActionStop(Ar),
            EEventActionType.SetGameParameter or
                EEventActionType.ResetGameParameter => new AkActionSetGameParameter(Ar),
            EEventActionType.SetHighPassFilter or
                EEventActionType.SetHighPassFilter2 or
                EEventActionType.ResetVoiceLowPassFilter or
                EEventActionType.ResetBusVolume or
                EEventActionType.SetVoiceVolume or
                EEventActionType.SetVoicePitch or
                EEventActionType.SetBusVolume or
                EEventActionType.SetVoiceLowPassFilter or
                EEventActionType.ResetVoiceVolume or
                EEventActionType.ResetVoicePitch => new AkActionSetAkProps(Ar),
            EEventActionType.Seek => new AkActionSeek(Ar),
            EEventActionType.SetSwitch => new AkActionSetSwitch(Ar),
            EEventActionType.SetState => new AkActionSetState(Ar),
            EEventActionType.Mute or
                EEventActionType.UnMute or
                EEventActionType.ResetPlaylist => new AkActionBase(Ar),
            EEventActionType.Resume => new AkActionResume(Ar),
            EEventActionType.Pause => new AkActionPause(Ar),
            // TODO: add all action types
            _ => null,
        };
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("EventActionScope");
        writer.WriteValue(EventActionScope.ToString());

        writer.WritePropertyName("EventActionType");
        writer.WriteValue(EventActionType.ToString());

        if (ReferencedId != 0)
        {
            writer.WritePropertyName("ReferencedId");
            writer.WriteValue(ReferencedId);
        }

        writer.WritePropertyName("Props");
        serializer.Serialize(writer, Props);

        writer.WritePropertyName("PropRanges");
        serializer.Serialize(writer, PropRanges);

        if (ActionData != null)
        {
            writer.WritePropertyName("ActionData");
            serializer.Serialize(writer, ActionData);
        }

        writer.WriteEndObject();
    }
}
