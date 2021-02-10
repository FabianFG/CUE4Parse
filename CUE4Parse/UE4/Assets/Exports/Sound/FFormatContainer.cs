using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Sound
{
    [JsonConverter(typeof(FFormatContainerConverter))]
    public class FFormatContainer
    {
        public SortedDictionary<FName, FByteBulkData> Formats;

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
            
            serializer.Serialize(writer, value.Formats);

            writer.WriteEndObject();
        }

        public override FFormatContainer ReadJson(JsonReader reader, Type objectType, FFormatContainer existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
