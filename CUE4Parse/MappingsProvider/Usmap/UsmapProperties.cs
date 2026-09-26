namespace CUE4Parse.MappingsProvider.Usmap;

public static class UsmapProperties
{
    public static Struct ParseStruct(TypeMappings context, FUsmapReader Ar)
    {
        if (Ar.Version >= EUsmapVersion.ExtendedMetadata)
            _ = Ar.ReadName(); // owner package name (or null)
        var name = Ar.ReadName()!;
        var superType = Ar.ReadName();
        var flags = Ar.Version >= EUsmapVersion.ExtendedMetadata ? Ar.Read<uint>() : 0;

        var propCountClassFlag = Ar.Version >= EUsmapVersion.ExtendedMetadata ? Ar.Read<int>() : Ar.Read<ushort>();
        var serializablePropertyCount = Ar.Version >= EUsmapVersion.ExtendedMetadata ? Ar.Read<int>() : Ar.Read<ushort>();
        var properties = new Dictionary<int, PropertyInfo>();
        for (var i = 0; i < serializablePropertyCount; i++)
        {
            var propInfo = ParsePropertyInfo(Ar);
            for (var j = 0; j < propInfo.ArraySize; j++)
            {
                var clone = (PropertyInfo) propInfo.Clone();
                clone.Index = j;
                properties[propInfo.Index + j] = clone;
            }
        }

        return new Struct(context, name, superType, properties, propCountClassFlag, flags);
    }

    public static PropertyInfo ParsePropertyInfo(FUsmapReader Ar)
    {
        var index = Ar.Read<ushort>();
        var arrayDim = Ar.Version >= EUsmapVersion.ExtendedMetadata ? Ar.Read<ushort>() : Ar.Read<byte>();
        var name = Ar.ReadName()!;
        var type = ParsePropertyType(Ar);
        var flags = Ar.ReadPropertyFlags(name);
        return new PropertyInfo(index, name, type, arrayDim, flags);
    }

    public static PropertyType ParsePropertyType(FUsmapReader Ar)
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
                innerType = ParsePropertyType(Ar);
                enumName = Ar.ReadName();
                break;
            case EPropertyType.StructProperty:
                structType = Ar.ReadName();
                break;
            case EPropertyType.SetProperty:
            case EPropertyType.ArrayProperty:
            case EPropertyType.OptionalProperty:
                innerType = ParsePropertyType(Ar);
                break;
            case EPropertyType.MapProperty:
                innerType = ParsePropertyType(Ar);
                valueType = ParsePropertyType(Ar);
                break;
        }

        return new PropertyType(type, structType, innerType, valueType, enumName, isEnumAsByte);
    }
}