using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Niagara;
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

            if (!Ar.HasUnversionedProperties && Ar.Versions.MapStructTypes.TryGetValue(tagData.Name, out var mapStructTypes))
            {
                if (!string.IsNullOrEmpty(mapStructTypes.Key)) tagData.InnerTypeData = new FPropertyTagData(mapStructTypes.Key);
                if (!string.IsNullOrEmpty(mapStructTypes.Value)) tagData.ValueTypeData = new FPropertyTagData(mapStructTypes.Value);
            }

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
            writer.WriteStartArray();

            foreach (var kvp in value.Properties)
            {
                switch (kvp.Key)
                {
                    case StructProperty:
                        writer.WriteStartObject();
                        writer.WritePropertyName("Key");
                        serializer.Serialize(writer, kvp.Key);
                        writer.WritePropertyName("Value");
                        serializer.Serialize(writer, kvp.Value);
                        writer.WriteEndObject();
                        break;
                    default:
                        writer.WriteStartObject();
                        writer.WritePropertyName(kvp.Key?.ToString().SubstringBefore('(').Trim() ?? "no key name???");
                        serializer.Serialize(writer, kvp.Value);
                        writer.WriteEndObject();
                        break;
                }
            }

            writer.WriteEndArray();
        }

        public override UScriptMap ReadJson(JsonReader reader, Type objectType, UScriptMap existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
