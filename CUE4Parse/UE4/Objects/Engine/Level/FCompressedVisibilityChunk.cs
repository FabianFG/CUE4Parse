using System;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.Engine.Level
{
    [JsonConverter(typeof(FCompressedVisibilityChunkConverter))]
    public readonly struct FCompressedVisibilityChunk : IUStruct
    {
        public readonly bool bCompressed;
        public readonly int UncompressedSize;
        public readonly byte[] Data;
        
        public FCompressedVisibilityChunk(FAssetArchive Ar)
        {
            bCompressed = Ar.ReadBoolean();
            UncompressedSize = Ar.Read<int>();
            Data = Ar.ReadBytes(Ar.Read<int>());
        }
    }
    
    public class FCompressedVisibilityChunkConverter : JsonConverter<FCompressedVisibilityChunk>
    {
        public override void WriteJson(JsonWriter writer, FCompressedVisibilityChunk value, JsonSerializer serializer)
        {
            writer.WritePropertyName("bCompressed");
            writer.WriteValue(value.bCompressed);
            
            writer.WritePropertyName("UncompressedSize");
            writer.WriteValue(value.UncompressedSize);
        }

        public override FCompressedVisibilityChunk ReadJson(JsonReader reader, Type objectType, FCompressedVisibilityChunk existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}