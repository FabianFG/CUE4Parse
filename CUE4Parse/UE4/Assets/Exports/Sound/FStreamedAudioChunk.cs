using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Sound
{
    [JsonConverter(typeof(FStreamedAudioChunkConverter))]
    public class FStreamedAudioChunk
    {
        public readonly int DataSize;
        public readonly int AudioDataSize;
        public readonly FByteBulkData BulkData;

        public FStreamedAudioChunk(FAssetArchive Ar)
        {
            if (Ar.ReadBoolean()) // bCooked
            {
                BulkData = new FByteBulkData(Ar);
                DataSize = Ar.Read<int>();
                AudioDataSize = Ar.Read<int>();
            }
            else
            {
                Ar.Position -= sizeof(int);
                throw new ParserException(Ar, "StreamedAudioChunks must be cooked");
            }
        }
    }
    
    public class FStreamedAudioChunkConverter : JsonConverter<FStreamedAudioChunk>
    {
        public override void WriteJson(JsonWriter writer, FStreamedAudioChunk value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            writer.WritePropertyName("DataSize");
            writer.WriteValue(value.DataSize);
            
            writer.WritePropertyName("AudioDataSize");
            writer.WriteValue(value.AudioDataSize);
            
            writer.WritePropertyName("BulkData");
            serializer.Serialize(writer, value.BulkData);
            
            writer.WriteEndObject();
        }

        public override FStreamedAudioChunk ReadJson(JsonReader reader, Type objectType, FStreamedAudioChunk existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
