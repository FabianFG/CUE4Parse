using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using System.Collections.Generic;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(UScriptMapConverter))]
    public class UScriptMap
    {
        public Dictionary<FPropertyTagType?, FPropertyTagType?> Properties;

        public UScriptMap()
        {
            Properties = new Dictionary<FPropertyTagType?, FPropertyTagType?>();
        }

        public UScriptMap(FAssetArchive Ar, FPropertyTagData tagData)
        {
            if (tagData.InnerType == null || tagData.ValueType == null)
                throw new ParserException(Ar, "Can't serialize UScriptMap without key or value type");
            
            int numKeyToRemove = Ar.Read<int>();
            for (int i = 0; i < numKeyToRemove; i++)
            {
                FPropertyTagType.ReadPropertyTagType(Ar, tagData.InnerType, tagData, ReadType.MAP);
            }

            int numEntries = Ar.Read<int>();
            Properties = new Dictionary<FPropertyTagType?, FPropertyTagType?>(numEntries);
            for (int i = 0; i < numEntries; i++)
            {
                try
                {
                    Properties[FPropertyTagType.ReadPropertyTagType(Ar, tagData.InnerType, tagData.InnerTypeData, ReadType.MAP)] = FPropertyTagType.ReadPropertyTagType(Ar, tagData.ValueType, tagData.ValueTypeData, ReadType.MAP);
                }
                catch (ParserException e)
                {
                    throw new ParserException(Ar, $"Failed to read key/value pair for index {i} in map", e);
                }
            }
        }
    }
    
    public class UScriptMapConverter : JsonConverter<UScriptMap>
    {
        public override void WriteJson(JsonWriter writer, UScriptMap value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            foreach (var kvp in value.Properties)
            {
                FPropertyTagType? key;
                if (kvp.Key is StructProperty s && s.Value.StructType is FStructFallback f && f.Properties.Count > 0)
                    key = f.Properties[0].Tag;
                else
                    key = kvp.Key;
                
                if (key == null) continue;
                writer.WritePropertyName(key.ToString().SubstringBefore('(').Trim());
                serializer.Serialize(writer, kvp.Value);
            }
            
            writer.WriteEndObject();
        }

        public override UScriptMap ReadJson(JsonReader reader, Type objectType, UScriptMap existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
