using System.Text;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class FPropertyTagData
    {
        public string? Name;
        public string Type;
        public string? StructType;
        public FGuid? StructGuid;
        public bool? Bool;
        public string? EnumName;
        public bool IsEnumAsByte;
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
            }
        }

        internal FPropertyTagData(PropertyType info)
        {
            Type = info.Type;
            StructType = info.StructType;
            StructGuid = null;
            Bool = info.Bool;
            EnumName = info.EnumName;
            IsEnumAsByte = info.IsEnumAsByte == true;
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
                    sb.AppendFormat("<{0}>", InnerTypeData?.ToString() ?? InnerType);
                    break;
                case "MapProperty":
                    sb.AppendFormat("<{0}, {1}>", InnerTypeData?.ToString() ?? InnerType, ValueTypeData?.ToString() ?? ValueType);
                    break;
            }

            return sb.ToString();
        }
    }
}