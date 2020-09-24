using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Objects
{
    public enum ReadType
    {
        NORMAL,
        MAP,
        ARRAY
    }

    public class FPropertyTagType
    {
        internal static FPropertyTagType? ReadPropertyTagType(FAssetArchive Ar, string propertyType, FPropertyTagData? tagData, ReadType type)
        {
            return propertyType switch
            {
                "ByteProperty" => null,
                "BoolProperty" => null,
                "IntProperty" => null,
                "FloatProperty" => null,
                "ObjectProperty" => null,
                "NameProperty" => null,
                "DelegateProperty" => null,
                "DoubleProperty" => null,
                "ArrayProperty" => null,
                "StructProperty" => null,
                "StrProperty" => null,
                "TextProperty" => null,
                "InterfaceProperty" => null,
                "SoftObjectProperty" => null,
                "AssetObjectProperty" => null,
                "UInt64Property" => null,
                "UInt32Property" => null,
                "UInt16Property" => null,
                "Int64Property" => null,
                "Int16Property" => null,
                "Int8Property" => null,
                "MapProperty" => null,
                "SetProperty" => null,
                "EnumProperty" => null,
                _ => null
            };
        }
    }
}
