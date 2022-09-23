using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.MappingsProvider.Usmap;

public static class UsmapProperties
{
    public static Struct ParseStruct(TypeMappings context, FArchive Ar, EUsmapVersion version, IReadOnlyList<string> nameLut)
    {
        var name = Ar.ReadName(nameLut)!;
        var superType = Ar.ReadName(nameLut);

        var propertyCount = Ar.Read<ushort>();
        var serializablePropertyCount = Ar.Read<ushort>();
        var properties = new Dictionary<int, PropertyInfo>();
        for (var i = 0; i < serializablePropertyCount; i++)
        {
            var propInfo = ParsePropertyInfo(Ar, version, nameLut);
            for (var j = 0; j < propInfo.ArraySize; j++)
            {
                properties[propInfo.Index + j] = propInfo;
            }
        }

        return new Struct(context, name, superType, properties, propertyCount);
    }

    public static PropertyInfo ParsePropertyInfo(FArchive Ar, EUsmapVersion version, IReadOnlyList<string> nameLut)
    {
        var index = Ar.Read<ushort>();
        var arrayDim = Ar.Read<byte>();
        var name = Ar.ReadName(nameLut)!;
        var type = ParsePropertyType(Ar, version, nameLut);
        return new PropertyInfo(index, name, type, arrayDim);
    }

    public static PropertyType ParsePropertyType(FArchive Ar, EUsmapVersion version, IReadOnlyList<string> nameLut)
    {
        var type = (version >= EUsmapVersion.PropertyTypeToClassFlags ? Enum.GetName(Ar.Read<EClassCastFlags>())?[1..] : Enum.GetName(Ar.Read<EPropertyType>())) ?? string.Empty;
        string? structType = null;
        PropertyType? innerType = null;
        PropertyType? valueType = null;
        string? enumName = null;
        bool? isEnumAsByte = null;

        switch (type)
        {
            case "EnumProperty":
                innerType = ParsePropertyType(Ar, version, nameLut);
                enumName = Ar.ReadName(nameLut);
                break;
            case "StructProperty":
                structType = Ar.ReadName(nameLut);
                break;
            case "SetProperty":
            case "ArrayProperty":
                innerType = ParsePropertyType(Ar, version, nameLut);
                break;
            case "MapProperty":
                innerType = ParsePropertyType(Ar, version, nameLut);
                valueType = ParsePropertyType(Ar, version, nameLut);
                break;
        }

        return new PropertyType(type, structType, innerType, valueType, enumName, isEnumAsByte);
    }
}