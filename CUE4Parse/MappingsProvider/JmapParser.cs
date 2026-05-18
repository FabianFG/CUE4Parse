using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using CUE4Parse.MappingsProvider.Usmap;
using CUE4Parse.Utils;

namespace CUE4Parse.MappingsProvider;

public class JmapParser
{
    public readonly TypeMappings? Mappings;

    public JmapParser(string path, StringComparer? comparer = null)
    {
        var data = File.ReadAllBytes(path);
        if (Path.GetExtension(path).Equals(".gz", StringComparison.OrdinalIgnoreCase))
        {
            using var decompressed = new MemoryStream();
            using var gzipStream = new GZipStream(new MemoryStream(data), CompressionMode.Decompress, leaveOpen: false);
            gzipStream.CopyTo(decompressed);
            decompressed.Position = 0;
            data = decompressed.ToArray();
        }

        Mappings = Read(data, comparer);
    }

    public JmapParser(byte[] data, StringComparer? comparer = null)
    {
        Mappings = Read(data, comparer);
    }

    private TypeMappings Read(byte[] data, StringComparer? comparer = null)
    {
        var enums = new Dictionary<string, Dictionary<long, string>>(1024);
        var structs = new Dictionary<string, Struct>(8192, comparer ?? StringComparer.OrdinalIgnoreCase);
        var mappings = new TypeMappings(structs, enums);

        var reader = new Utf8JsonReader(data);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            return mappings;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var propertyName = reader.GetString();
            if (!reader.Read())
                break;

            if (propertyName is "objects" && reader.TokenType == JsonTokenType.StartObject)
            {
                ReadObjects(ref reader, mappings);
            }
            else
            {
                reader.Skip();
            }
        }

        return mappings;
    }

    private void ReadObjects(ref Utf8JsonReader reader, TypeMappings mappings)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var name = reader.GetString();
            if (name is null || !reader.Read())
                break;

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                AddObjectMapping(ref reader, mappings, name);
            }
            else
            {
                reader.Skip();
            }
        }
    }

    private void AddObjectMapping(ref Utf8JsonReader reader, TypeMappings mappings, string name)
    {
        string? type = null;
        string? superType = null;
        Dictionary<long, string>? enumValues = null;
        Dictionary<int, PropertyInfo>? properties = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var propertyName = reader.GetString();
            if (!reader.Read())
                break;

            switch (propertyName)
            {
                case "type" when reader.TokenType == JsonTokenType.String:
                    type = reader.GetString();
                    break;
                case "names" when reader.TokenType == JsonTokenType.StartArray:
                    enumValues = ReadEnumValues(ref reader);
                    break;
                case "super_struct" when reader.TokenType == JsonTokenType.String:
                    superType = reader.GetString()?.SubstringAfterLast('.');
                    break;
                case "properties" when reader.TokenType == JsonTokenType.StartArray:
                    properties = ReadProperties(ref reader);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        switch (type)
        {
            case "Enum" when enumValues is not null:
                mappings.Enums[name.SubstringAfterLast('.')] = enumValues;
                break;
            case "Class":
            case "ScriptStruct":
                properties ??= new Dictionary<int, PropertyInfo>();
                var typeName = name.SubstringAfterLast('.');
                mappings.Types[typeName] = new Struct(mappings, typeName, superType, properties, properties.Count);
                break;
        }
    }

    private Dictionary<long, string> ReadEnumValues(ref Utf8JsonReader reader)
    {
        var values = new Dictionary<long, string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            if (reader.TokenType == JsonTokenType.StartArray)
                ReadEnumValue(ref reader, values);
            else
                reader.Skip();
        }

        return values;
    }

    private void ReadEnumValue(ref Utf8JsonReader reader, Dictionary<long, string> values)
    {
        string? enumName = null;
        long? enumIndex = null;
        var index = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (index == 0 && reader.TokenType == JsonTokenType.String)
                enumName = reader.GetString();
            else if (index == 1 && reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out var value))
                enumIndex = value;
            else
                reader.Skip();

            index++;
        }

        if (enumName is not null && enumIndex.HasValue)
            values[enumIndex.Value] = enumName;
    }

    private Dictionary<int, PropertyInfo> ReadProperties(ref Utf8JsonReader reader)
    {
        var properties = new Dictionary<int, PropertyInfo>();
        var index = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                reader.Skip();
                continue;
            }

            var propertyInfo = ParsePropertyInfo(ref reader);
            properties[index++] = propertyInfo;
            for (var i = 1; i < propertyInfo.ArraySize; i++)
            {
                var clone = (PropertyInfo) propertyInfo.Clone();
                clone.Index = i;
                properties[index++] = clone;
            }
        }

        return properties;
    }

    private PropertyInfo ParsePropertyInfo(ref Utf8JsonReader reader)
    {
        var property = ReadProperty(ref reader);
        return new PropertyInfo(0, property.Name ?? string.Empty, ToPropertyType(property), property.ArrayDim);
    }

    private JmapProperty ReadProperty(ref Utf8JsonReader reader)
    {
        var property = new JmapProperty();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            var propertyName = reader.GetString();
            if (!reader.Read())
                break;

            switch (propertyName)
            {
                case "name" when reader.TokenType == JsonTokenType.String:
                    property.Name = reader.GetString();
                    break;
                case "array_dim" when reader.TokenType == JsonTokenType.Number:
                    property.ArrayDim = reader.GetInt32();
                    break;
                case "type" when reader.TokenType == JsonTokenType.String:
                    property.Type = reader.GetString();
                    break;
                case "struct" when reader.TokenType == JsonTokenType.String:
                    property.StructType = reader.GetString()?.SubstringAfterLast('.');
                    break;
                case "enum" when reader.TokenType == JsonTokenType.String:
                    property.EnumName = reader.GetString()?.SubstringAfterLast('.');
                    break;
                case "container" or "inner" or "key_prop" when reader.TokenType == JsonTokenType.StartObject:
                    property.InnerType = ReadProperty(ref reader);
                    break;
                case "value_prop" when reader.TokenType == JsonTokenType.StartObject:
                    property.ValueType = ReadProperty(ref reader);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return property;
    }

    private PropertyType ToPropertyType(JmapProperty property)
    {
        var type = property.Type ?? string.Empty;
        if (!Enum.TryParse<EPropertyType>(type, out var propertyType))
            propertyType = EPropertyType.Unknown;
        else
            type = Enum.GetName(propertyType) ?? type;

        PropertyType? innerType = property.InnerType is not null ? ToPropertyType(property.InnerType) : null;
        PropertyType? valueType = property.ValueType is not null ? ToPropertyType(property.ValueType) : null;

        switch (propertyType)
        {
            case EPropertyType.EnumProperty:
                innerType ??= new PropertyType(nameof(EPropertyType.ByteProperty));
                break;
            case EPropertyType.ArrayProperty:
            case EPropertyType.SetProperty:
            case EPropertyType.OptionalProperty:
                innerType ??= new PropertyType(nameof(EPropertyType.Unknown));
                break;
            case EPropertyType.MapProperty:
                innerType ??= new PropertyType(nameof(EPropertyType.Unknown));
                valueType ??= new PropertyType(nameof(EPropertyType.Unknown));
                break;
        }

        return new PropertyType(type, property.StructType, innerType, valueType, property.EnumName);
    }

    private sealed class JmapProperty
    {
        public string? Name;
        public int ArrayDim = 1;
        public string? Type;
        public string? StructType;
        public JmapProperty? InnerType;
        public JmapProperty? ValueType;
        public string? EnumName;
    }
}
