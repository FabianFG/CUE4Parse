using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Objects.Actions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

// CAkAction::Create
// CAkAction::SetInitialValues
public class HierarchyEventAction : AbstractHierarchy
{
    public readonly EEventActionScope EventActionScope;
    public readonly EEventActionType EventActionType;
    public readonly byte IsBus;
    public readonly uint ReferencedId;
    public readonly AkProp[] Props;
    public readonly AkPropRange[] PropRanges;
    public readonly object? ActionData;

    public HierarchyEventAction(FArchive Ar) : base(Ar)
    {
        EventActionScope = Ar.Read<EEventActionScope>();
        EventActionType = Ar.Read<EEventActionType>();
        ReferencedId = Ar.Read<uint>();
        IsBus = Ar.Read<byte>();

        var propBundle = new AkPropBundle(Ar);
        Props = propBundle.Props;
        PropRanges = propBundle.PropRanges;

        ActionData = (EventActionType, WwiseVersions.Version) switch
        {
            (EEventActionType.Play, _) => new AkActionPlay(Ar),
            (EEventActionType.Stop, _) => new AkActionStop(Ar),
            (EEventActionType.SetGameParameter or
                EEventActionType.ResetGameParameter, _) => new AkActionSetGameParameter(Ar),
            (EEventActionType.SetHighPassFilter or
                EEventActionType.ResetHighPassFilter or
                EEventActionType.ResetVoiceLowPassFilter or
                EEventActionType.ResetBusVolume or
                EEventActionType.SetVoiceVolume or
                EEventActionType.SetVoicePitch or
                EEventActionType.SetBusVolume or
                EEventActionType.SetVoiceLowPassFilter or
                EEventActionType.ResetVoiceVolume or
                EEventActionType.ResetVoicePitch, _) => new AkActionSetAkProps(Ar),
            (EEventActionType.Seek, _) => new AkActionSeek(Ar),
            (EEventActionType.SetSwitch, _) => new AkActionSetSwitch(Ar),
            (EEventActionType.SetState, _) => new AkActionSetState(Ar),
            (EEventActionType.SetEffect or
                EEventActionType.ResetEffect, _) => new AkActionSetEffect(Ar),
            (EEventActionType.Mute or
                EEventActionType.UnMute or
                EEventActionType.ResetPlaylist, _) => new AkActionBase(Ar),
            (EEventActionType.Resume, _) => new AkActionResume(Ar),
            (EEventActionType.Pause, _) => new AkActionPause(Ar),
            (EEventActionType.Break or
                EEventActionType.Trigger, < 150) => new AkActionBypassFX(Ar),
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
        writer.WriteValue(EventActionType.ToString(WwiseVersions.Version));

        if (ReferencedId != 0)
        {
            writer.WritePropertyName("ReferencedId");
            writer.WriteValue(ReferencedId);
        }

        writer.WritePropertyName("IsBus");
        writer.WriteValue(IsBus != 0);

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
