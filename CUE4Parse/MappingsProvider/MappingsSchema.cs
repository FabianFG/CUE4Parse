using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace CUE4Parse.MappingsProvider
{
    public class Struct
    {
        public readonly TypeMappings Context;
        public string Name;
        public string? SuperType;
        public Lazy<Struct?> Super;
        public Dictionary<int, PropertyInfo> Properties;
        public int PropertyCount;

        public Struct(TypeMappings context, string name, string? superType, Dictionary<int, PropertyInfo> properties, int propertyCount)
        {
            Context = context;
            Name = name;
            SuperType = superType;
            Super = new Lazy<Struct?>(() =>
            {
                if (SuperType != null && Context.Types.TryGetValue(SuperType, out var superStruct))
                {
                    return superStruct;
                }

                return null;
            });
            Properties = properties;
            PropertyCount = propertyCount;
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

        public PropertyInfo(int index, string name, PropertyType mappingType, int? arraySize = null)
        {
            Index = index;
            Name = name;
            ArraySize = arraySize;
            MappingType = mappingType;
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

        public PropertyType(string type, string? structType = null, PropertyType? innerType = null, PropertyType? valueType = null, string? enumName = null, bool? isEnumAsByte = null, bool? b = null)
        {
            Type = type;
            StructType = structType;
            InnerType = innerType;
            ValueType = valueType;
            EnumName = enumName;
            IsEnumAsByte = isEnumAsByte;
            Bool = b;
        }
        
    }
}