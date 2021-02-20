using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(FStructFallbackConverter))]
    public class FStructFallback : IUStruct, IPropertyHolder
    {
        public List<FPropertyTag> Properties { get; }

        public FStructFallback()
        {
            Properties = new List<FPropertyTag>();
        }

        public FStructFallback(FAssetArchive Ar, string? structType)
        {
            if (Ar.HasUnversionedProperties)
            {
                if (structType == null) throw new ArgumentException("For unversioned struct fallback the struct type cannot be null", nameof(structType));
                Properties = UObject.DeserializePropertiesUnversioned(Ar, structType);
            }
            else
            {
                Properties = UObject.DeserializePropertiesTagged(Ar);
            }
        }
        
        public T GetOrDefault<T>(string name, T defaultValue = default, StringComparison comparisonType = StringComparison.Ordinal) =>
            PropertyUtil.GetOrDefault<T>(this, name, defaultValue, comparisonType);
        public FPropertyTagType? GetOrNull<T>(string name, StringComparison comparisonType = StringComparison.Ordinal) =>
            PropertyUtil.GetOrNull<T>(this, name, comparisonType);
        public T Get<T>(string name, StringComparison comparisonType = StringComparison.Ordinal) =>
            PropertyUtil.Get<T>(this, name, comparisonType);
    }
    
    public class FStructFallbackConverter : JsonConverter<FStructFallback>
    {
        public override void WriteJson(JsonWriter writer, FStructFallback value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            foreach (var property in value.Properties)
            {
                writer.WritePropertyName(property.Name.Text);
                serializer.Serialize(writer, property.Tag);
            }
            
            writer.WriteEndObject();
        }

        public override FStructFallback ReadJson(JsonReader reader, Type objectType, FStructFallback existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
