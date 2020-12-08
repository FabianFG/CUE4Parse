using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class FPropertyTagData
    {
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

        internal FPropertyTagData(FAssetArchive Ar, string type)
        {
            Type = type;
            switch (type)
            {
                case "StructProperty":
                    StructType = Ar.ReadFName().Text;
                    if (Ar.Ver >= UE4Version.VER_UE4_STRUCT_GUID_IN_PROPERTY_TAG)
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
                    if (Ar.Ver >= UE4Version.VAR_UE4_ARRAY_PROPERTY_INNER_TAGS)
                        InnerType = Ar.ReadFName().Text;
                    break;
                // Serialize the following if version is past VER_UE4_PROPERTY_TAG_SET_MAP_SUPPORT
                case "SetProperty":
                    if (Ar.Ver >= UE4Version.VER_UE4_PROPERTY_TAG_SET_MAP_SUPPORT)
                        InnerType = Ar.ReadFName().Text;
                    break;
                case "MapProperty":
                    if (Ar.Ver >= UE4Version.VER_UE4_PROPERTY_TAG_SET_MAP_SUPPORT)
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
        }
    }
}
