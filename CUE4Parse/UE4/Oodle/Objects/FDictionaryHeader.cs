using System;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Oodle.Objects
{
    [JsonConverter(typeof(FDictionaryHeaderConverter))]
    public class FDictionaryHeader
    {
        public readonly uint Magic;
        public readonly uint DictionaryVersion;
        public readonly uint OodleMajorHeaderVersion;
        public readonly int HashTableSize;
        public readonly FOodleCompressedData DictionaryData;
        public readonly FOodleCompressedData CompressorData;

        public FDictionaryHeader(FArchive Ar)
        {
            Magic = Ar.Read<uint>();
            DictionaryVersion = Ar.Read<uint>();
            OodleMajorHeaderVersion = Ar.Read<uint>();
            HashTableSize = Ar.Read<int>();
            DictionaryData = Ar.Read<FOodleCompressedData>();
            CompressorData = Ar.Read<FOodleCompressedData>();
        }
    }
    
    public class FDictionaryHeaderConverter : JsonConverter<FDictionaryHeader>
    {
        public override void WriteJson(JsonWriter writer, FDictionaryHeader value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            writer.WritePropertyName("Magic");
            serializer.Serialize(writer, value.Magic);
            
            writer.WritePropertyName("DictionaryVersion");
            serializer.Serialize(writer, value.DictionaryVersion);
            
            writer.WritePropertyName("OodleMajorHeaderVersion");
            serializer.Serialize(writer, value.OodleMajorHeaderVersion);

            writer.WritePropertyName("HashTableSize");
            serializer.Serialize(writer, value.HashTableSize);
            
            writer.WritePropertyName("DictionaryData");
            serializer.Serialize(writer, value.DictionaryData);
            
            writer.WritePropertyName("CompressorData");
            serializer.Serialize(writer, value.CompressorData);
            
            writer.WriteEndObject();
        }

        public override FDictionaryHeader ReadJson(JsonReader reader, Type objectType, FDictionaryHeader existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}