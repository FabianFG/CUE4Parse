using System;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(InterfacePropertyConverter))]
    public class InterfaceProperty : FPropertyTagType<UInterfaceProperty>
    {
        public InterfaceProperty(FArchive Ar, ReadType type)
        {
            Value = type switch
            {
                ReadType.ZERO => new UInterfaceProperty(),
                _ => Ar.Read<UInterfaceProperty>()
            };
        }
    }
    
    public class InterfacePropertyConverter : JsonConverter<InterfaceProperty>
    {
        public override void WriteJson(JsonWriter writer, InterfaceProperty value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Value.InterfaceNumber); // use serializer if more variables are being added to UInterfaceProperty
        }

        public override InterfaceProperty ReadJson(JsonReader reader, Type objectType, InterfaceProperty existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}