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
            
            var numKeysToRemove = Ar.Read<int>();
            for (var i = 0; i < numKeysToRemove; i++)
            {
                FPropertyTagType.ReadPropertyTagType(Ar, tagData.InnerType, tagData.InnerTypeData, ReadType.MAP);
            }

            var numEntries = Ar.Read<int>();
            Properties = new Dictionary<FPropertyTagType?, FPropertyTagType?>(numEntries);
            for (var i = 0; i < numEntries; i++)
            {
                var isReadingValue = false;
                try
                {
                    var key = FPropertyTagType.ReadPropertyTagType(Ar, tagData.InnerType, tagData.InnerTypeData, ReadType.MAP);
                    isReadingValue = true;
                    var value = FPropertyTagType.ReadPropertyTagType(Ar, tagData.ValueType, tagData.ValueTypeData, ReadType.MAP);
                    Properties[key] = value;
                }
                catch (ParserException e)
                {
                    throw new ParserException(Ar, $"Failed to read {(isReadingValue ? "value" : "key")} for index {i} in map", e);
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
                FPropertyTagType? key = null;
                if (kvp.Key is StructProperty s1 && s1.Value.StructType is FStructFallback f)
                {
                    foreach (var prop in f.Properties)
                    {
                        if (prop.Tag is StructProperty s2 && s2.Value.StructType is FStructFallback) continue;
                        key = prop.Tag;
                        break;
                    }
                }
                else key = kvp.Key;
                
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
