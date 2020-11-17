using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class FPropertyTagData
    {
        public string? StructType;
        public FGuid? StructGuid;
        public bool? Bool;
        public string? EnumName;
        public string? EnumType;
        public string? InnerType;
        public string? ValueType;

        internal FPropertyTagData(FAssetArchive Ar, string type)
        {
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

        internal FPropertyTagData(PropertyInfo info)
        {
            StructType = info.StructType;
            StructGuid = null;
            Bool = info.Bool;
            EnumName = info.EnumName;
            EnumType = info.EnumType;
            InnerType = info.InnerType;
            ValueType = info.ValueType;
        }
    }
}
