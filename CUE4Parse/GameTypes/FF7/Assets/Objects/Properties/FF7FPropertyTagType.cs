using System;
using System.Collections.Generic;
using CUE4Parse.GameTypes.FF7.Assets.Exports;
using CUE4Parse.GameTypes.FF7.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.FF7.Assets.Objects.Properties;

public enum FF7propertyType : int
{
    BoolProperty = 1,
    ByteProperty = 2,
    Int8Property = 3,
    UInt16Property = 4,
    Int16Property = 5,
    UIntProperty = 6,
    IntProperty = 7,
    Int64Property = 8,
    FloatProperty = 9,
    StrProperty = 10,
    NameProperty = 11,
}

public class FF7BoolProperty : FF7FPropertyTagType<bool>
{
    public FF7BoolProperty(FMemoryMappedImageArchive Ar)
    {
        Value = Ar.ReadFlag();
    }
}

public class FF7ByteProperty : FF7FPropertyTagType<byte>
{
    public FF7ByteProperty(FMemoryMappedImageArchive Ar)
    {
        Value = Ar.Read<byte>();
    }
}

public class FF7Int8Property : FF7FPropertyTagType<sbyte>
{
    public FF7Int8Property(FMemoryMappedImageArchive Ar)
    {
        Value = Ar.Read<sbyte>();
    }
}

public class FF7UInt16Property : FF7FPropertyTagType<ushort>
{
    public FF7UInt16Property(FMemoryMappedImageArchive Ar)
    {
        Value = Ar.Read<ushort>();
    }
}

public class FF7Int16Property : FF7FPropertyTagType<short>
{
    public FF7Int16Property(FMemoryMappedImageArchive Ar)
    {
        Value = Ar.Read<short>();
    }
}

public class FF7UIntProperty : FF7FPropertyTagType<uint>
{
    public FF7UIntProperty(FMemoryMappedImageArchive Ar)
    {
        Ar.Position = Ar.Position.Align(4);
        Value = Ar.Read<uint>();
    }
}

public class FF7IntProperty : FF7FPropertyTagType<int>
{
    public FF7IntProperty(FMemoryMappedImageArchive Ar)
    {
        Ar.Position = Ar.Position.Align(4);
        Value = Ar.Read<int>();
    }
}

public class FF7Int64Property : FF7FPropertyTagType<long>
{
    public FF7Int64Property(FMemoryMappedImageArchive Ar)
    {
        Value = Ar.Read<long>();
    }
}

public class FF7FloatProperty : FF7FPropertyTagType<float>
{
    public FF7FloatProperty(FMemoryMappedImageArchive Ar)
    {
        Ar.Position = Ar.Position.Align(4);
        Value = Ar.Read<float>();
    }
}

public class FF7StrProperty : FF7FPropertyTagType<string>
{
    public FF7StrProperty(FMemoryMappedImageArchive Ar)
    {
        Ar.Position = Ar.Position.Align(8);
        Value = Ar.ReadFString();
    }
}

public class FF7NameProperty : FF7FPropertyTagType<FName>
{
    public FF7NameProperty(FMemoryMappedImageArchive Ar)
    {
        Ar.Position = Ar.Position.Align(4);
        Value = Ar.ReadFName();
    }
}

[JsonConverter(typeof(FF7ArrayPropertyConverter))]
public class FF7ArrayProperty : FPropertyTagType<FPropertyTagType[]>
{
    public FF7ArrayProperty(FMemoryMappedImageArchive Ar, FF7propertyType underlyingType)
    {
        Ar.Position = Ar.Position.Align(8);
        var align = underlyingType switch
        {
            FF7propertyType.BoolProperty => 1,
            FF7propertyType.ByteProperty => 1,
            FF7propertyType.Int8Property => 1,
            FF7propertyType.UInt16Property => 2,
            FF7propertyType.Int16Property => 2,
            _ => 4,
        };
        Value = Ar.ReadArray(() => Ar.ReadPropertyTagType(underlyingType), align);
    }
}

public class FF7ArrayPropertyConverter : JsonConverter<FF7ArrayProperty>
{
    public override void WriteJson(JsonWriter writer, FF7ArrayProperty value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        foreach (var prop in value.Value)
        {
            serializer.Serialize(writer, prop, prop.GetType());
        }
        writer.WriteEndArray();
    }

    public override FF7ArrayProperty ReadJson(JsonReader reader, Type objectType, FF7ArrayProperty existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

[JsonConverter(typeof(FF7StructPropertyConverter) )]
public class FF7StructProperty : FPropertyTagType<List<FPropertyTagType>>
{
    public FF7Property[] StructDefinition;

    public FF7StructProperty(List<FPropertyTagType> properties, FF7Property[] structDefinition)
    {
        Value = properties;
        StructDefinition = structDefinition;
    }
}

public class FF7StructPropertyConverter : JsonConverter<FF7StructProperty>
{
    public override void WriteJson(JsonWriter writer, FF7StructProperty value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        for (var i = 0; i < value.Value.Count; i++)
        {
            writer.WritePropertyName(value.StructDefinition[i].Name.Text);
            serializer.Serialize(writer, value.Value[i], value.Value[i].GetType());
        }
        writer.WriteEndObject();
    }

    public override FF7StructProperty ReadJson(JsonReader reader, Type objectType, FF7StructProperty existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

[JsonConverter(typeof(FF7FPropertyTagTypeConverter))]
public abstract class FF7FPropertyTagType<T> : FPropertyTagType<T>;

public class FF7FPropertyTagTypeConverter : JsonConverter
{
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(FF7FPropertyTagType<>);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var type = value.GetType();
        var valueProperty = type.GetProperty("Value"); // Get the `Value` property
        var valueToSerialize = valueProperty?.GetValue(value);

        serializer.Serialize(writer, valueToSerialize);
    }
}
