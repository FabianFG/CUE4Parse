using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

[StructFallback]
[JsonConverter(typeof(FWwiseEventCookedDataConverter))]
public readonly struct FWwiseEventCookedData
{
    public readonly uint EventId;
    public readonly FWwiseSoundBankCookedData[] SoundBanks;
    public readonly FWwiseMediaCookedData[] Media;
    public readonly FWwiseExternalSourceCookedData[] ExternalSources;
    public readonly Dictionary<FWwiseGroupValueCookedDataSet, FWwiseAudioNodeCookedData?> AudioNodes;
    public readonly FWwiseSwitchContainerLeafCookedData[] SwitchContainerLeaves;
    public readonly FWwiseGroupValueCookedData[] RequiredGroupValueSet;
    public readonly EWwiseEventDestroyOptions DestroyOptions;
    public readonly FName DebugName;

    public FWwiseEventCookedData(FStructFallback fallback)
    {
        EventId = (uint)fallback.GetOrDefault<int>(nameof(EventId), comparisonType: StringComparison.OrdinalIgnoreCase);
        SoundBanks = fallback.GetOrDefault<FWwiseSoundBankCookedData[]>(nameof(SoundBanks), []);
        Media = fallback.GetOrDefault<FWwiseMediaCookedData[]>(nameof(Media), []);
        ExternalSources = fallback.GetOrDefault<FWwiseExternalSourceCookedData[]>(nameof(ExternalSources), []);
        AudioNodes = fallback.GetOrDefault<Dictionary<FWwiseGroupValueCookedDataSet, FWwiseAudioNodeCookedData?>>(nameof(AudioNodes), []);
        SwitchContainerLeaves = fallback.GetOrDefault<FWwiseSwitchContainerLeafCookedData[]>(nameof(SwitchContainerLeaves), []);
        RequiredGroupValueSet = fallback.GetOrDefault<FWwiseGroupValueCookedData[]>(nameof(RequiredGroupValueSet), []);
        DestroyOptions = fallback.GetOrDefault<EWwiseEventDestroyOptions>(nameof(DestroyOptions));
        DebugName = fallback.GetOrDefault<FName>(nameof(DebugName));
    }

    public void SerializeBulkData(FAssetArchive Ar)
    {
        foreach (var sb in SoundBanks)
            sb.SerializeBulkData(Ar);

        foreach (var media in Media)
            media.SerializeBulkData(Ar);

        foreach (var audioNode in AudioNodes.Values)
            audioNode?.SerializeBulkData(Ar);

        foreach (var leaf in SwitchContainerLeaves)
            leaf.SerializeBulkData(Ar);
    }
}

public class FWwiseEventCookedDataConverter : JsonConverter<FWwiseEventCookedData>
{
    public override void WriteJson(JsonWriter writer, FWwiseEventCookedData value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(value.EventId));
        writer.WriteValue(value.EventId);
        writer.WritePropertyName(nameof(value.SoundBanks));
        serializer.Serialize(writer, value.SoundBanks);
        writer.WritePropertyName(nameof(value.Media));
        serializer.Serialize(writer, value.Media);
        writer.WritePropertyName(nameof(value.ExternalSources));
        serializer.Serialize(writer, value.ExternalSources);
        writer.WritePropertyName(nameof(value.AudioNodes));
        writer.WriteStartArray();
        foreach (var (group, node) in value.AudioNodes)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Key");
            serializer.Serialize(writer, group);
            writer.WritePropertyName("Value");
            serializer.Serialize(writer, node);

            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WritePropertyName(nameof(value.SwitchContainerLeaves));
        serializer.Serialize(writer, value.SwitchContainerLeaves);
        writer.WritePropertyName(nameof(value.RequiredGroupValueSet));
        serializer.Serialize(writer, value.RequiredGroupValueSet);
        writer.WritePropertyName(nameof(value.DestroyOptions));
        serializer.Serialize(writer, value.DestroyOptions);
        writer.WritePropertyName(nameof(value.DebugName));
        serializer.Serialize(writer, value.DebugName);

        writer.WriteEndObject();
    }

    public override FWwiseEventCookedData ReadJson(JsonReader reader, Type objectType, FWwiseEventCookedData existingValue, bool hasExistingValue, JsonSerializer serializer) =>
        throw new NotImplementedException();
}
