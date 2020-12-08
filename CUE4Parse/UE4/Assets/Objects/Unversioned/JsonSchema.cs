using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace CUE4Parse.UE4.Assets.Objects.Unversioned
{
    public class Struct
    {
        public readonly TypeMappings Context;
        public string Name;
        public string? SuperType;
        public Lazy<Struct?> Super;
        public Dictionary<int, PropertyInfo> Properties;
        public int PropertyCount;

        public Struct(TypeMappings context, JToken structToken)
        {
            Context = context;
            Name = structToken["name"]!.ToObject<string>()!;
            SuperType = structToken["superType"]?.ToObject<string>();
            Super = new Lazy<Struct?>(() =>
            {
                if (SuperType != null && Context.Types.TryGetValue(SuperType, out var superStruct))
                {
                    return superStruct;
                }

                return null;
            });

            var propertiesToken = (JArray) structToken["properties"]!;
            var properties = new Dictionary<int, PropertyInfo>();
            Properties = properties;
            foreach (var propToken in propertiesToken)
            {
                if (propToken == null) continue;
                var prop = new PropertyInfo(propToken);
                properties[prop.Index] = prop;
            }
            PropertyCount = structToken["propertyCount"]!.ToObject<int>()!;
        }

        public bool TryGetValue(int i, out PropertyInfo info)
        {
            if (!Properties.TryGetValue(i, out info))
            {
                return i >= PropertyCount && Super.Value != null && 
                       Super.Value.TryGetValue(i - PropertyCount, out info);
            }

            return true;
        }
    }

    public class PropertyInfo
    {
        public int Index;
        public string Name;
        public int? ArraySize;
        public PropertyType MappingType;

        public PropertyInfo(JToken propToken)
        {
            Index = propToken["index"]!.ToObject<int>()!;
            Name = propToken["name"]!.ToObject<string>()!;
            ArraySize = propToken["arraySize"]?.ToObject<int>();
            MappingType = new PropertyType(propToken["mappingType"]!);
        }
    }

    public class PropertyType
    {
        public string Type;
        public string? StructType;
        public PropertyType? InnerType;
        public PropertyType? ValueType;
        public string? EnumName;
        public bool? IsEnumAsByte;
        public bool? Bool;

        public PropertyType(JToken typeToken)
        {
            Type = typeToken["type"]!.ToObject<string>()!;
            StructType = typeToken["structType"]?.ToObject<string>();
            var innerTypeToken = typeToken["innerType"];
            InnerType = innerTypeToken != null ? new PropertyType(innerTypeToken) : null;
            var valueTypeToken = typeToken["valueType"];
            ValueType = valueTypeToken != null ? new PropertyType(valueTypeToken) : null;
            EnumName = typeToken["enumName"]?.ToObject<string>();
            IsEnumAsByte = typeToken["isEnumAsByte"]?.ToObject<bool>();
        }
    }
}