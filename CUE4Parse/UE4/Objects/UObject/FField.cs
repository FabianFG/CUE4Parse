using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;

namespace CUE4Parse.UE4.Objects.UObject
{
    public class FField
    {
        public FName Name;
        public uint Flags;

        public virtual void Deserialize(FAssetArchive Ar)
        {
            Name = Ar.ReadFName();
            Flags = Ar.Read<uint>();
        }

        public override string ToString() => Name.Text;

        public static FField Construct(FName fieldTypeName) => fieldTypeName.Text switch
        {
            "ArrayProperty" => new FArrayProperty(),
            "BoolProperty" => new FBoolProperty(),
            "ByteProperty" => new FByteProperty(),
            "ClassProperty" => new FClassProperty(),
            "DelegateProperty" => new FDelegateProperty(),
            "EnumProperty" => new FEnumProperty(),
            "FloatProperty" => new FFloatProperty(),
            "Int16Property" => new FInt16Property(),
            "Int64Property" => new FInt64Property(),
            "Int8Property" => new FInt8Property(),
            "IntProperty" => new FIntProperty(),
            "InterfaceProperty" => new FInterfaceProperty(),
            "MapProperty" => new FMapProperty(),
            "MulticastDelegateProperty" => new FMulticastDelegateProperty(),
            "MulticastInlineDelegateProperty" => new FMulticastInlineDelegateProperty(),
            "NameProperty" => new FNameProperty(),
            "ObjectProperty" => new FObjectProperty(),
            "SetProperty" => new FSetProperty(),
            "SoftClassProperty" => new FSoftClassProperty(),
            "SoftObjectProperty" => new FSoftObjectProperty(),
            "StrProperty" => new FStrProperty(),
            "StructProperty" => new FStructProperty(),
            "TextProperty" => new FTextProperty(),
            "UInt16Property" => new FUInt16Property(),
            "UInt32Property" => new FUInt32Property(),
            "UInt64Property" => new FUInt64Property(),
            _ => throw new ParserException("Unsupported serialized property type " + fieldTypeName)
        };

        public static FField? SerializeSingleField(FAssetArchive Ar)
        {
            var propertyTypeName = Ar.ReadFName();
            if (!propertyTypeName.IsNone)
            {
                var field = Construct(propertyTypeName);
                field.Deserialize(Ar);
                return field;
            }
            return null;
        }
    }
}