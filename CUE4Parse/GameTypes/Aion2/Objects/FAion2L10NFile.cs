using System;
using System.Collections.Generic;
using CUE4Parse.FileProvider.Objects;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.Aion2.Objects;

[JsonConverter(typeof(FAion2L10NConverter))]
public class FAion2L10NFile
{
    public string Namespace = string.Empty;
    public Dictionary<string, string> Entries = [];

    public FAion2L10NFile(GameFile file)
    {
        using var Ar = file.SafeCreateReader();
        if (Ar is null) return;

        Namespace = Ar.ReadFString();
        Entries = Ar.ReadMap(Ar.ReadFString, Ar.ReadFString);
    }
}

public class FAion2L10NConverter : JsonConverter<FAion2L10NFile>
{
    public override FAion2L10NFile? ReadJson(JsonReader reader, Type objectType, FAion2L10NFile? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, FAion2L10NFile? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(value.Namespace));
        writer.WriteValue(value.Namespace);
        writer.WritePropertyName(nameof(value.Entries));
        serializer.Serialize(writer, value.Entries);

        writer.WriteEndObject();
    }
}
