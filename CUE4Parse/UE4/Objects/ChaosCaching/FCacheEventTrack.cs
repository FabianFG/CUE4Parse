using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.ChaosCaching;

[JsonConverter(typeof(FCacheEventTrackConverter))]
public class FCacheEventTrack : FStructFallback
{
    public FStructFallback[]? Events;

    public FCacheEventTrack(FAssetArchive Ar) : base(Ar, "CacheEventTrack")
    {
        var strukt = GetOrDefault<FPackageIndex>("Struct");
        var count = GetOrDefault<float[]>("TimeStamps")?.Length ?? 0;
        if (strukt.TryLoad<UStruct>(out var Struct))
        {
            Events = Ar.ReadArray(count, () => new FStructFallback(Ar, Struct));
        }
    }
}

public class FCacheEventTrackConverter : JsonConverter<FCacheEventTrack>
{
    public override void WriteJson(JsonWriter writer, FCacheEventTrack value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        foreach (var property in value.Properties)
        {
            writer.WritePropertyName(property.ArrayIndex > 0 ? $"{property.Name.Text}[{property.ArrayIndex}]" : property.Name.Text);
            serializer.Serialize(writer, property.Tag);
        }
        writer.WritePropertyName("Events");
        serializer.Serialize(writer, value.Events);
        writer.WriteEndObject();
    }
    public override FCacheEventTrack ReadJson(JsonReader reader, Type objectType, FCacheEventTrack existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
