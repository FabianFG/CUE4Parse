using System;
using System.Text;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Objects;

public class FPropertyTagData
{
    public string? Name;
    public string Type;
    public string? StructType;
    public string? Module;
    public FGuid? StructGuid;
    public bool? Bool;
    public string? EnumName;
    //public bool IsEnumAsByte;
    public string? InnerType;
    public string? ValueType;
    public FPropertyTagData? InnerTypeData;
    public FPropertyTagData? ValueTypeData;
    public UStruct? Struct;
    public UEnum? Enum;

    internal FPropertyTagData(FAssetArchive Ar, string type, string name = "")
    {
        Name = name;
        Type = type;
        switch (type)
        {
            case "StructProperty":
                StructType = Ar.ReadFName().Text;
                if (Ar.Ver >= EUnrealEngineObjectUE4Version.STRUCT_GUID_IN_PROPERTY_TAG)
                    StructGuid = Ar.Read<FGuid>();
                break;
            case "BoolProperty":
                Bool = Ar.ReadFlag();
                break;
            case "ByteProperty":
            case "EnumProperty":
                EnumName = Ar.ReadFName().Text;
                break;
            case "ArrayProperty":
                if (Ar.Ver >= EUnrealEngineObjectUE4Version.ARRAY_PROPERTY_INNER_TAGS)
                    InnerType = Ar.ReadFName().Text;
                break;
            // Serialize the following if version is past PROPERTY_TAG_SET_MAP_SUPPORT
            case "SetProperty":
                if (Ar.Ver >= EUnrealEngineObjectUE4Version.PROPERTY_TAG_SET_MAP_SUPPORT)
                    InnerType = Ar.ReadFName().Text;
                break;
            case "MapProperty":
                if (Ar.Ver >= EUnrealEngineObjectUE4Version.PROPERTY_TAG_SET_MAP_SUPPORT)
                {
                    InnerType = Ar.ReadFName().Text;
                    ValueType = Ar.ReadFName().Text;
                }
                break;
            case "OptionalProperty":
                InnerType = Ar.ReadFName().Text;
                break;
        }
    }

    internal FPropertyTagData(Span<FPropertyTypeNameNode> typeName, string name = "")
    {
        Name = name;
        Type = typeName.GetName();
        switch (Type)
        {
            case "BoolProperty":
                Bool = false;
                break;
            case "StructProperty":
                if (typeName.GetParameter(0) is { IsEmpty: false } structType)
                {
                    StructType = structType.GetName();
                    Module = structType.GetParameter(0).GetName();
                    // doesn't use StructGuid anyway
                    // if (typeName.GetParameter(1) is { } guid) StructGuid = new Guid(guid.GetName);
                }
                break;
            case "ByteProperty":
            case "EnumProperty":
                if (typeName.GetParameter(0) is { IsEmpty: false } enumType)
                {
                    EnumName = enumType.GetName();
                    Module = enumType.GetParameter(0).GetName();
                }
                break;
            case "ArrayProperty":
            case "SetProperty":
            case "OptionalProperty":
                if (typeName.GetParameter(0) is { IsEmpty: false } innerType)
                {
                    InnerType = innerType.GetName();
                    InnerTypeData = InnerType != "None" && innerType[0].InnerCount != 0 ? new FPropertyTagData(innerType, InnerType) : null;
                }
                break;
            case "MapProperty":
                if (typeName.GetParameter(0) is { IsEmpty: false } keyType && typeName.GetParameter(1) is { IsEmpty: false } valueType)
                {
                    InnerType = keyType.GetName();
                    InnerTypeData = InnerType != "None" && keyType[0].InnerCount != 0 ? new FPropertyTagData(keyType, InnerType) : null;
                    ValueType = valueType.GetName();
                    ValueTypeData = InnerType != "None" && valueType[0].InnerCount != 0 ? new FPropertyTagData(valueType, ValueType) : null;
                }
                break;
        }
    }

    internal FPropertyTagData(PropertyType info)
    {
        Type = info.Type;
        StructType = info.StructType;
        StructGuid = null;
        Bool = info.Bool;
        EnumName = info.EnumName;
        //IsEnumAsByte = info.IsEnumAsByte == true;
        InnerTypeData = info.InnerType != null ? new FPropertyTagData(info.InnerType) : null;
        InnerType = InnerTypeData?.Type;
        ValueTypeData = info.ValueType != null ? new FPropertyTagData(info.ValueType) : null;
        ValueType = ValueTypeData?.Type;
        Struct = info.Struct;
        Enum = info.Enum;
    }

    internal FPropertyTagData(string structType, string name = "")
    {
        Name = name;
        Type = "StructProperty";
        StructType = structType;
    }

    public override string ToString()
    {
        var sb = new StringBuilder(Type);
        switch (Type)
        {
            case "StructProperty":
                sb.AppendFormat("<{0}>", StructType);
                break;
            case "ByteProperty" when EnumName != null:
            case "EnumProperty":
                sb.AppendFormat("<{0}>", EnumName);
                break;
            case "ArrayProperty":
            case "SetProperty":
            case "OptionalProperty":
                sb.AppendFormat("<{0}>", InnerTypeData?.ToString() ?? InnerType);
                break;
            case "MapProperty":
                sb.AppendFormat("<{0}, {1}>", InnerTypeData?.ToString() ?? InnerType, ValueTypeData?.ToString() ?? ValueType);
                break;
        }

        return sb.ToString();
    }
}
