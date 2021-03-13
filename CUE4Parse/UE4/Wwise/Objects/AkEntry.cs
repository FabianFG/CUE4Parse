using System;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects
{
    [JsonConverter(typeof(AkEntryConverter))]
    public class AkEntry
    {
        public readonly uint NameHash;
        public readonly uint OffsetMultiplier;
        public readonly int Size;
        public readonly uint Offset;
        public readonly uint FolderId;
        public string Path;
        public bool IsSoundBank;
        public byte[] Data;
        
        public AkEntry(FArchive Ar)
        {
            NameHash = Ar.Read<uint>();
            OffsetMultiplier = Ar.Read<uint>();
            Size = Ar.Read<int>();
            Offset = Ar.Read<uint>();
            FolderId = Ar.Read<uint>();
        }
    }
    
    public class AkEntryConverter : JsonConverter<AkEntry>
    {
        public override void WriteJson(JsonWriter writer, AkEntry value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            writer.WritePropertyName("NameHash");
            writer.WriteValue(value.NameHash);
            
            writer.WritePropertyName("OffsetMultiplier");
            writer.WriteValue(value.OffsetMultiplier);
            
            writer.WritePropertyName("Size");
            writer.WriteValue(value.Size);
            
            writer.WritePropertyName("Offset");
            writer.WriteValue(value.Offset);
            
            writer.WritePropertyName("FolderId");
            writer.WriteValue(value.FolderId);
            
            writer.WritePropertyName("Path");
            writer.WriteValue(value.Path);
            
            writer.WritePropertyName("IsSoundBank");
            writer.WriteValue(value.IsSoundBank);
            
            writer.WriteEndObject();
        }

        public override AkEntry ReadJson(JsonReader reader, Type objectType, AkEntry existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}