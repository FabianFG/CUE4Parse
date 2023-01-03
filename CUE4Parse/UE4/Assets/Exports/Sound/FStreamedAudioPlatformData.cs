using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Sound
{
    [JsonConverter(typeof(FStreamedAudioPlatformDataConverter))]
    public class FStreamedAudioPlatformData
    {
        public readonly int NumChunks;
        public readonly FName AudioFormat;
        public readonly FStreamedAudioChunk[] Chunks;

        public FStreamedAudioPlatformData(FAssetArchive Ar)
        {
            NumChunks = Ar.Read<int>();
            AudioFormat = Ar.ReadFName();
            Chunks = Ar.ReadArray(NumChunks, () => new FStreamedAudioChunk(Ar));
        }
    }

    public class FStreamedAudioPlatformDataConverter : JsonConverter<FStreamedAudioPlatformData>
    {
        public override void WriteJson(JsonWriter writer, FStreamedAudioPlatformData value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("NumChunks");
            writer.WriteValue(value.NumChunks);

            writer.WritePropertyName("AudioFormat");
            serializer.Serialize(writer, value.AudioFormat);

            writer.WritePropertyName("Chunks");
            serializer.Serialize(writer, value.Chunks);

            writer.WriteEndObject();
        }

        public override FStreamedAudioPlatformData ReadJson(JsonReader reader, Type objectType, FStreamedAudioPlatformData existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}