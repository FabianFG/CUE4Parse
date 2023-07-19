using System;
using Newtonsoft.Json;

namespace CUE4Parse.UE4;

[JsonConverter(typeof(UnrealBaseConverter))]
public abstract class UnrealBase
{
    protected internal abstract void WriteJson(JsonWriter writer, JsonSerializer serializer);
}

public class UnrealBaseConverter : JsonConverter<UnrealBase>
{
    public override void WriteJson(JsonWriter writer, UnrealBase? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        value?.WriteJson(writer, serializer);
        writer.WriteEndObject();
    }

    public override UnrealBase ReadJson(JsonReader reader, Type objectType, UnrealBase? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public interface IUStruct;
