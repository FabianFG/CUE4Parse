using System;
using CUE4Parse.UE4.Assets.Exports;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.Aion2.Objects;

[JsonConverter(typeof(FAion2DataFileConverter))]
public abstract class FAion2DataFile : AbstractPropertyHolder
{
    public int Version = 0;
    public string[] Ids = [];
    public FAion2DataFile() { }
}

public class FAion2DataFileConverter : JsonConverter<FAion2DataFile>
{
    public override FAion2DataFile? ReadJson(JsonReader reader, Type objectType, FAion2DataFile? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, FAion2DataFile? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(value.Version));
        serializer.Serialize(writer, value.Version);

        writer.WritePropertyName(nameof(value.Ids));
        serializer.Serialize(writer, value.Ids);

        if (value.Properties.Count > 0)
        {
            writer.WritePropertyName(nameof(value.Properties));
            writer.WriteStartObject();
            foreach (var property in value.Properties)
            {
                writer.WritePropertyName(property.ArrayIndex > 0 ? $"{property.Name.Text}[{property.ArrayIndex}]" : property.Name.Text);
                serializer.Serialize(writer, property.Tag);
            }
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}
