using System;
using System.Collections.Generic;
using CUE4Parse.UE4;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.FF7.Objects
{
    [JsonConverter(typeof(FEndTextResourceStringsConverter))]
    public class FEndTextResourceStrings : IUStruct
    {
        public readonly Dictionary<string, string>? Entries;

        public FEndTextResourceStrings(FArchive Ar)
        {
            var str = Ar.ReadFString();
            var length = Ar.Read<int>();

            if (length > 0)
            {
                Entries = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(str)) Entries.Add("Str", str);
                for (var i = 0; i < length; i++)
                {
                    var key = Ar.ReadFName();
                    var val = Ar.ReadFString();
                    Entries.Add(key.Text, val);
                }
            }
        }
    }

    public class FEndTextResourceStringsConverter : JsonConverter<FEndTextResourceStrings>
    {
        public override void WriteJson(JsonWriter writer, FEndTextResourceStrings value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            if (value.Entries?.Count > 0)
            {
                writer.WritePropertyName("Entries");
                serializer.Serialize(writer, value.Entries);
            }

            writer.WriteEndObject();
        }

        public override FEndTextResourceStrings ReadJson(JsonReader reader, Type objectType, FEndTextResourceStrings? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
