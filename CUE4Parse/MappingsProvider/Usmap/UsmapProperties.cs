using System;
using System.Collections.Generic;

namespace CUE4Parse.MappingsProvider.Usmap
{
    public static class UsmapProperties
    {
        public static Struct ParseStruct(TypeMappings context, FUsmapReader Ar, IReadOnlyList<string> nameLut)
        {
            var name = Ar.ReadName(nameLut)!;
            var superType = Ar.ReadName(nameLut);

            var propertyCount = Ar.Read<ushort>();
            var serializablePropertyCount = Ar.Read<ushort>();
            var properties = new Dictionary<int, PropertyInfo>();
            for (var i = 0; i < serializablePropertyCount; i++)
            {
                var propInfo = ParsePropertyInfo(Ar, nameLut);
                for (var j = 0; j < propInfo.ArraySize; j++)
                {
                    var clone = (PropertyInfo) propInfo.Clone();
                    clone.Index = j;
                    properties[propInfo.Index + j] = clone;
                }
            }

            return new Struct(context, name, superType, properties, propertyCount);
        }

        public static PropertyInfo ParsePropertyInfo(FUsmapReader Ar, IReadOnlyList<string> nameLut)
        {
            var index = Ar.Read<ushort>();
            var arrayDim = Ar.Read<byte>();
            var name = Ar.ReadName(nameLut)!;
            var type = ParsePropertyType(Ar, nameLut);
            return new PropertyInfo(index, name, type, arrayDim);
        }

        public static PropertyType ParsePropertyType(FUsmapReader Ar, IReadOnlyList<string> nameLut)
        {
            var typeEnum = Ar.Read<EPropertyType>();
            var type = Enum.GetName(typeEnum) ?? string.Empty;
            string? structType = null;
            PropertyType? innerType = null;
            PropertyType? valueType = null;
            string? enumName = null;
            bool? isEnumAsByte = null;

            switch (typeEnum)
            {
                case EPropertyType.EnumProperty:
                    innerType = ParsePropertyType(Ar, nameLut);
                    enumName = Ar.ReadName(nameLut);
                    break;
                case EPropertyType.StructProperty:
                    structType = Ar.ReadName(nameLut);
                    break;
                case EPropertyType.SetProperty:
                case EPropertyType.ArrayProperty:
                    innerType = ParsePropertyType(Ar, nameLut);
                    break;
                case EPropertyType.MapProperty:
                    innerType = ParsePropertyType(Ar, nameLut);
                    valueType = ParsePropertyType(Ar, nameLut);
                    break;
            }

            return new PropertyType(type, structType, innerType, valueType, enumName, isEnumAsByte);
        }
    }
}
