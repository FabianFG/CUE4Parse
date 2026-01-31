using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Objects.Actions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

// CAkAction::Create
// CAkAction::SetInitialValues
public class HierarchyEventAction : AbstractHierarchy
{
    public readonly EAkActionScope EventActionScope;
    public readonly EAkActionType EventActionType;
    public readonly byte IsBus;
    public readonly uint ReferencedId;
    public readonly AkProp[] Props;
    public readonly AkPropRange[] PropRanges;
    public readonly object? ActionData;

    public HierarchyEventAction(FArchive Ar) : base(Ar)
    {
        EventActionScope = Ar.Read<EAkActionScope>();
        EventActionType = Ar.Read<EAkActionType>();
        ReferencedId = Ar.Read<uint>();
        IsBus = Ar.Read<byte>();

        var propBundle = new AkPropBundle(Ar);
        Props = propBundle.Props;
        PropRanges = propBundle.PropRanges;

        ActionData = (EventActionType, WwiseVersions.Version) switch
        {
            (EAkActionType.Play, _) => new CAkActionPlay(Ar),
            (EAkActionType.Stop, _) => new CAkActionStop(Ar),
            (EAkActionType.SetGameParameter or
                EAkActionType.ResetGameParameter, _) => new CAkActionSetGameParameter(Ar),
            (EAkActionType.SetHighPassFilter or
                EAkActionType.ResetHighPassFilter or
                EAkActionType.ResetVoiceLowPassFilter or
                EAkActionType.ResetBusVolume or
                EAkActionType.SetVoiceVolume or
                EAkActionType.SetVoicePitch or
                EAkActionType.SetBusVolume or
                EAkActionType.SetVoiceLowPassFilter or
                EAkActionType.ResetVoiceVolume or
                EAkActionType.ResetVoicePitch, _) => new CAkActionSetAkProp(Ar),
            (EAkActionType.Seek, _) => new CAkActionSeek(Ar),
            (EAkActionType.SetSwitch, _) => new CAkActionSetSwitch(Ar),
            (EAkActionType.SetState, _) => new CAkActionSetState(Ar),
            (EAkActionType.SetEffect or
                EAkActionType.ResetEffect, _) => new CAkActionSetFX(Ar),
            (EAkActionType.Mute or
                EAkActionType.UnMute or
                EAkActionType.ResetPlaylist, _) => new CAkActionBase(Ar),
            (EAkActionType.Resume, _) => new CAkActionResume(Ar),
            (EAkActionType.Pause, _) => new CAkActionPause(Ar),
            (EAkActionType.Break or
                EAkActionType.Trigger, < 150) => new CAkActionBypassFX(Ar),
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
