using System;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Material
{
    [StructFallback, JsonConverter(typeof(FMaterialParameterInfoConverter))]
    public class FMaterialParameterInfo
    {
        public FName Name;
        public EMaterialParameterAssociation Association;
        public int Index;

        public FMaterialParameterInfo(FStructFallback fallback)
        {
            Name = fallback.GetOrDefault<FName>(nameof(Name));
            Association = fallback.GetOrDefault<EMaterialParameterAssociation>(nameof(Association));
            Index = fallback.GetOrDefault<int>(nameof(Index));
        }

        public FMaterialParameterInfo(FArchive Ar)
        {
            Name = Ar.ReadFName();
            Association = Ar.Read<EMaterialParameterAssociation>();
            Index = Ar.Read<int>();
        }

        public FMaterialParameterInfo()
        {
            Name = new FName();
            Association = EMaterialParameterAssociation.LayerParameter;
            Index = 0;
        }
    }

    public class FMaterialParameterInfoConverter : JsonConverter<FMaterialParameterInfo>
    {
        public override void WriteJson(JsonWriter writer, FMaterialParameterInfo value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Name");
            serializer.Serialize(writer, value.Name);

            writer.WritePropertyName("Association");
            writer.WriteValue($"EMaterialParameterAssociation::{value.Association.ToString()}");

            writer.WritePropertyName("Index");
            writer.WriteValue(value.Index);

            writer.WriteEndObject();
        }

        public override FMaterialParameterInfo ReadJson(JsonReader reader, Type objectType, FMaterialParameterInfo existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public enum EMaterialParameterAssociation : byte
    {
        LayerParameter,
        BlendParameter,
        GlobalParameter
    }
}
