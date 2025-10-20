using System;
using System.Text;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.Psychonauts2.Assets.Objects.Properties;

[JsonConverter(typeof(LinecodePropertyConverter))]
public class LinecodeProperty : FPropertyTagType<string>
{
    public LinecodeProperty(FAssetArchive Ar, ReadType type)
    {
        Value = type switch
        {
            ReadType.ZERO => string.Empty,
            _ => Encoding.UTF8.GetString(Ar.ReadBytes(13)).TrimEnd('\0')
        };
    }
}

public class LinecodePropertyConverter : JsonConverter<LinecodeProperty>
{
    public override void WriteJson(JsonWriter writer, LinecodeProperty value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value.Value);
    }

    public override LinecodeProperty ReadJson(JsonReader reader, Type objectType, LinecodeProperty? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
