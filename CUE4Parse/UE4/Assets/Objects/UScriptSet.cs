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
        public readonly string InnerType; 
        public readonly FPropertyTagData? InnerTagData;
        public readonly List<FPropertyTagType> Properties;
        
        public UScriptSet(string innerType)
        {
            InnerType = innerType;
            InnerTagData = null;
            Properties = new List<FPropertyTagType>();
        }
        
        public UScriptSet(FAssetArchive Ar, FPropertyTagData? tagData)
        {
            InnerType = tagData?.InnerType ?? throw new ParserException(Ar, "UScriptSet needs inner type");

            var numKeyToRemove = Ar.Read<int>();
            for (var i = 0; i < numKeyToRemove; i++)
            {
                FPropertyTagType.ReadPropertyTagType(Ar, tagData.InnerType, tagData, ReadType.ARRAY);
            }
            
            if (Ar.HasUnversionedProperties)
            {
                InnerTagData = tagData.InnerTypeData;
            }
            else if (InnerType == "StructProperty" || InnerType == "SetProperty")
            {
                InnerTagData = new FPropertyTag(Ar, false).TagData;
                if (InnerTagData == null)
                    throw new ParserException(Ar, $"Couldn't read SetProperty with inner type {InnerType}");
            }
            
            var numEntries = Ar.Read<int>();
            Properties = new List<FPropertyTagType>(numEntries);
            for (var i = 0; i < numEntries; i++)
            {
                var property = FPropertyTagType.ReadPropertyTagType(Ar, InnerType, InnerTagData, ReadType.ARRAY);
                if (property != null)
                    Properties.Add(property);
                else
                    Log.Debug($"Failed to read set property of type {InnerType} at ${Ar.Position}, index {i}");
            }
        }

        public override string ToString() => $"{InnerType}[{Properties.Count}]";
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