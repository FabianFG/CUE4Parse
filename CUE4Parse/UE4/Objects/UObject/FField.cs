using CUE4Parse.GameTypes.Borderlands4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.UObject;

[JsonConverter(typeof(FFieldConverter))]
public class FField
{
    public FName Name;
    public EObjectFlags Flags;

    public virtual void Deserialize(FAssetArchive Ar)
    {
        Name = Ar.ReadFName();
        Flags = Ar.Read<EObjectFlags>();
    }

    protected internal virtual void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WritePropertyName("Type");
        serializer.Serialize(writer, GetType().Name[1..]);

        writer.WritePropertyName("Name");
        serializer.Serialize(writer, Name);

        if (Flags != 0)
        {
            writer.WritePropertyName("Flags");
            writer.WriteValue(Flags.ToStringBitfield());
        }
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
        "FieldPathProperty" => new FFieldPathProperty(),
        "DoubleProperty" => new FDoubleProperty(),
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
        "ObjectProperty" or "ObjectPtrProperty" => new FObjectProperty(),
        "SetProperty" => new FSetProperty(),
        "SoftClassProperty" => new FSoftClassProperty(),
        "SoftObjectProperty" => new FSoftObjectProperty(),
        "StrProperty" => new FStrProperty(),
        "Utf8StrProperty" => new FUtf8StrProperty(),
        "StructProperty" => new FStructProperty(),
        "TextProperty" => new FTextProperty(),
        "UInt16Property" => new FUInt16Property(),
        "UInt32Property" => new FUInt32Property(),
        "UInt64Property" => new FUInt64Property(),
        "WeakObjectProperty" => new FWeakObjectProperty(),
        "OptionalProperty" => new FOptionalProperty(),
        "VerseStringProperty" => new FVerseStringProperty(),
        "VerseClassProperty" => new FVerseClassProperty(),
        "VerseFunctionProperty" => new FVerseFunctionProperty(),
        "VerseDynamicProperty" => new FVerseDynamicProperty(),
        "ReferenceProperty" => new FReferenceProperty(),

        "GameDataHandleProperty" => new FGameDataHandleProperty(),
        "GbxDefPtrProperty" => new FGbxDefPtrProperty(),
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
