using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(UScriptSetConverter))]
    public class UScriptSet
    {
        public readonly List<FPropertyTagType> Properties;

        public UScriptSet()
        {
            Properties = new List<FPropertyTagType>();
        }

        public UScriptSet(FAssetArchive Ar, FPropertyTagData? tagData)
        {
            var innerType = tagData?.InnerType ?? throw new ParserException(Ar, "UScriptSet needs inner type");

            var numElementsToRemove = Ar.Read<int>();
            for (var i = 0; i < numElementsToRemove; i++)
            {
                FPropertyTagType.ReadPropertyTagType(Ar, innerType, tagData.InnerTypeData, ReadType.ARRAY);
            }

            var num = Ar.Read<int>();
            Properties = new List<FPropertyTagType>(num);
            for (var i = 0; i < num; i++)
            {
                var property = FPropertyTagType.ReadPropertyTagType(Ar, innerType, tagData.InnerTypeData, ReadType.ARRAY);
                if (property != null)
                    Properties.Add(property);
                else
                    Log.Debug($"Failed to read element for index {i} in set");
            }
        }
    }

    public class UScriptSetConverter : JsonConverter<UScriptSet>
    {
        public override void WriteJson(JsonWriter writer, UScriptSet value, JsonSerializer serializer)
        {
            writer.WriteStartArray();

            foreach (var property in value.Properties)
            {
                serializer.Serialize(writer, property);
            }

            writer.WriteEndArray();
        }

        public override UScriptSet ReadJson(JsonReader reader, Type objectType, UScriptSet existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
