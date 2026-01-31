using System;
using System.Diagnostics;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

[JsonConverter(typeof(HierarchyConverter))]
public readonly struct Hierarchy
{
    public readonly EAKBKHircType Type;
    public readonly uint Length;
    public readonly AbstractHierarchy Data;

    public Hierarchy(FArchive Ar)
    {
        byte rawType = Ar.Read<byte>();
        Length = Ar.Read<uint>();
        var hierarchyEndPosition = Ar.Position + Length;

        Type = rawType.MapToCurrent();

        // Try/Catch is done to allow for extracting audio even if it fails
        // Due to their complexity it's very likely hierarchies will fail to parse if unsupported
        try
        {
            Data = Type switch
            {
                EAKBKHircType.State => new HierarchySettings(Ar),
                EAKBKHircType.SoundSfxVoice => new HierarchySoundSfxVoice(Ar),
                EAKBKHircType.EventAction => new HierarchyEventAction(Ar),
                EAKBKHircType.Event => new HierarchyEvent(Ar),
                EAKBKHircType.RandomSequenceContainer => new HierarchyRandomSequenceContainer(Ar),
                EAKBKHircType.SwitchContainer => new HierarchySwitchContainer(Ar),
                EAKBKHircType.ActorMixer => new HierarchyActorMixer(Ar),
                EAKBKHircType.AudioBus => new HierarchyAudioBus(Ar),
                EAKBKHircType.LayerContainer => new HierarchyLayerContainer(Ar),
                EAKBKHircType.MusicSegment => new HierarchyMusicSegment(Ar),
                EAKBKHircType.MusicTrack => new HierarchyMusicTrack(Ar),
                EAKBKHircType.MusicSwitchContainer => new HierarchyMusicSwitchContainer(Ar),
                EAKBKHircType.MusicRandomSequenceContainer => new HierarchyMusicRandomSequenceContainer(Ar),
                EAKBKHircType.Attenuation => new HierarchyAttenuation(Ar),
                EAKBKHircType.DialogueEvent => new HierarchyDialogueEvent(Ar),
                EAKBKHircType.FxShareSet => new HierarchyFxShareSet(Ar),
                EAKBKHircType.FxCustom => new HierarchyFxCustom(Ar),
                EAKBKHircType.AuxiliaryBus => new HierarchyAuxiliaryBus(Ar),
                EAKBKHircType.AudioDevice => new HierarchyAudioDevice(Ar),
                EAKBKHircType.LFO => new HierarchyLFO(Ar),
                EAKBKHircType.Envelope => new HierarchyEnvelope(Ar),
                EAKBKHircType.TimeMod => new HierarchyTimeMod(Ar),
                _ => new HierarchyGeneric(Ar)
            };
        }
        catch (Exception ex) when (!Debugger.IsAttached)
        {
            Log.Error(ex, "Failed to parse HIRC type {Type}. Falling back to generic.", Type);
            Ar.Position = hierarchyEndPosition - Length;
            Data = new HierarchyGeneric(Ar);
        }
        finally
        {
            if (Ar.Position != hierarchyEndPosition)
            {
#if DEBUG
                Log.Warning($"Didn't read hierarchy {Type} {Data?.Id} correctly (at {Ar.Position}, should be {hierarchyEndPosition})");
                if (Data is HierarchyEventAction action)
                {
                    Log.Warning($"EventAction type: {action.EventActionType}");
                }
#endif
                Ar.Position = hierarchyEndPosition;
            }
        }
    }
}

public class HierarchyConverter : JsonConverter<Hierarchy>
{
    public override void WriteJson(JsonWriter writer, Hierarchy value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(value.Type));
        writer.WriteValue(value.Type.ToVersionString());

#if DEBUG
        writer.WritePropertyName(nameof(value.Length));
        writer.WriteValue(value.Length.ToString());
#endif

        writer.WritePropertyName(nameof(value.Data.Id));
        writer.WriteValue(value.Data.Id.ToString());

        writer.WritePropertyName(nameof(value.Data));
        value.Data.WriteJson(writer, serializer);

        writer.WriteEndObject();
    }

    public override Hierarchy ReadJson(JsonReader reader, Type objectType, Hierarchy existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
