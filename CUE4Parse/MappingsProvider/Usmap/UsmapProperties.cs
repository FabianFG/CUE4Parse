using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.MappingsProvider.Usmap;

public static class UsmapProperties
{
    public static Struct ParseStruct(TypeMappings context, FArchive Ar, IReadOnlyList<string> nameLut)
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
                properties[propInfo.Index + j] = propInfo;
            }
        }

        return new Struct(context, name, superType, properties, propertyCount);
    }

    public static PropertyInfo ParsePropertyInfo(FArchive Ar, IReadOnlyList<string> nameLut)
    {
        var index = Ar.Read<ushort>();
        var arrayDim = Ar.Read<byte>();
        var name = Ar.ReadName(nameLut)!;
        var type = ParsePropertyType(Ar, nameLut);
        return new PropertyInfo(index, name, type, arrayDim);
    }

    public static PropertyType ParsePropertyType(FArchive Ar, IReadOnlyList<string> nameLut)
    {
        var typeEnum = Ar.Read<EPropertyType>();
        var type = Enum.GetName(typeof(EPropertyType), typeEnum)!;
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

    private enum EPropertyType : byte
    {
        ByteProperty,
        BoolProperty,
        IntProperty,
        FloatProperty,
        ObjectProperty,
        NameProperty,
        DelegateProperty,
        DoubleProperty,
        ArrayProperty,
        StructProperty,
        StrProperty,
        TextProperty,
        InterfaceProperty,
        MulticastDelegateProperty,
        WeakObjectProperty, //
        LazyObjectProperty, // When deserialized, these 3 properties will be SoftObjects
        AssetObjectProperty, //
        SoftObjectProperty,
        UInt64Property,
        UInt32Property,
        UInt16Property,
        Int64Property,
        Int16Property,
        Int8Property,
        MapProperty,
        SetProperty,
        EnumProperty,
        FieldPathProperty,

        Unknown = 0xFF
    }
}