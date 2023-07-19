using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject
{
    [JsonConverter(typeof(FFormatContainerConverter))]
    public class FFormatContainer
    {
        public readonly SortedDictionary<FName, FByteBulkData> Formats;

        public FFormatContainer(FAssetArchive Ar)
        {
            var numFormats = Ar.Read<int>();
            Formats = new SortedDictionary<FName, FByteBulkData>();
            for (var i = 0; i < numFormats; i++)
            {
                Formats[Ar.ReadFName()] = new FByteBulkData(Ar);
            }
        }
    }

    public class FFormatContainerConverter : JsonConverter<FFormatContainer>
    {
        public override void WriteJson(JsonWriter writer, FFormatContainer value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            foreach (var kvp in value.Formats)
            {
                writer.WritePropertyName(kvp.Key.Text);
                serializer.Serialize(writer, kvp.Value);
            }

            writer.WriteEndObject();
        }

        public override FFormatContainer ReadJson(JsonReader reader, Type objectType, FFormatContainer existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}