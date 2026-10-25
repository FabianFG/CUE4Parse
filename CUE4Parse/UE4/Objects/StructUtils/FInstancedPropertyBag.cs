using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.StructUtils;

[JsonConverter(typeof(EnumConverter<EPropertyBagPropertyType>))]
public enum EPropertyBagPropertyType : byte
{
    None,
    Bool,
    Byte,
    Int32,
    Int64,
    Float,
    Double,
    Name,
    String,
    Text,
    Enum,
    Struct,
    Object,
    SoftObject,
    Class,
    SoftClass,
    Int8,	// Type not fully supported at UI, will work with restrictions to type editing
    Int16,	// Type not fully supported at UI, will work with restrictions to type editing
    UInt16,	// Type not fully supported at UI, will work with restrictions to type editing
    UInt32,	// Type not fully supported at UI, will work with restrictions to type editing
    UInt64, // Type not fully supported at UI, will work with restrictions to type editing

    Count
};

[JsonConverter(typeof(EnumConverter<EPropertyBagContainerType>))]
public enum EPropertyBagContainerType : byte
{
    None,
    Array,
    Set,
    Map,
    Count
};

public class FInstancedPropertyBag : IUStruct
{
    public FPropertyBagPropertyDesc[] PropertyDescs = [];
    public int SerialSize;
    public FStructFallback Value = new();

    public FInstancedPropertyBag(FAssetArchive Ar)
    {
        if (this is FInstancedOverridablePropertyBag
            && FOverridablePropertyBagCustomVersion.Get(Ar) < FOverridablePropertyBagCustomVersion.Type.FixSerializer)
        {
            return;
        }

        var Version = EVersion.LatestVersion;
        if (FPropertyBagCustomVersion.Get(Ar) < FPropertyBagCustomVersion.Type.ContainerTypes)
        {
            Version = Ar.Read<EVersion>();
        }

        var bHasData = Ar.ReadBoolean();
        if (!bHasData)
            return;

        PropertyDescs = Ar.ReadArray(() => new FPropertyBagPropertyDesc(Ar));

        if (Version >= EVersion.SerializeStructSize)
            SerialSize = Ar.Read<int>();

        // The values are the properties of a struct built from the descriptors
        var payloadEnd = Ar.Position + SerialSize;
        var properties = new List<FPropertyTag>();
        try
        {
            if (Ar.HasUnversionedProperties)
                Assets.Exports.UObject.DeserializePropertiesUnversioned(properties, Ar, () => BuildSchema(Ar));
            else
                Assets.Exports.UObject.DeserializePropertiesTagged(properties, Ar, true);
        }
        catch (Exception e) when (Version >= EVersion.SerializeStructSize)
        {
            Log.Warning(e, "Failed to read FInstancedPropertyBag values, skipping {Size} bytes", SerialSize);
            Ar.Position = payloadEnd;
            return;
        }

        if (Version >= EVersion.SerializeStructSize)
        {
            var extra = Ar.Position - payloadEnd;
            Ar.Position = payloadEnd;
            if (extra > 0)
            {
                Log.Warning("FInstancedPropertyBag values ran {Extra} bytes past their size of {Size}, skipping them", extra, SerialSize);
                return;
            }

            // Not reaching the end means some value had the wrong size, like an enum with a wider underlying type
            if (extra < 0)
                Log.Warning("FInstancedPropertyBag values ended {Missing} bytes before their size of {Size}", -extra, SerialSize);
        }

        Value = new FStructFallback(properties);
    }

    // The descriptors in order are the schema, each one becomes the property UE creates for it
    private Struct BuildSchema(FAssetArchive Ar)
    {
        var properties = new Dictionary<int, PropertyInfo>(PropertyDescs.Length);
        for (var i = 0; i < PropertyDescs.Length; i++)
        {
            var desc = PropertyDescs[i];
            var propertyType = BuildPropertyType(Ar, desc.ValueType, desc.ValueTypeObject);

            // Containers wrap the value type, the first one is the outermost
            var containers = desc.ContainerTypes.Types ?? (desc.ContainerType != EPropertyBagContainerType.None ? [desc.ContainerType] : []);
            for (var c = containers.Length - 1; c >= 0; c--)
            {
                propertyType = containers[c] switch
                {
                    EPropertyBagContainerType.Array => new PropertyType("ArrayProperty", innerType: propertyType),
                    EPropertyBagContainerType.Set => new PropertyType("SetProperty", innerType: propertyType),
                    EPropertyBagContainerType.Map => new PropertyType("MapProperty",
                        innerType: BuildPropertyType(Ar, desc.KeyType, desc.KeyTypeObject), valueType: propertyType),
                    _ => propertyType
                };
            }

            properties[i] = new PropertyInfo(0, desc.Name.Text, propertyType, 1);
        }

        return new Struct(Ar.Owner?.Mappings, nameof(FInstancedPropertyBag), null, properties, PropertyDescs.Length);
    }

    // User defined structs and enums aren't in the mappings, so load the asset the desc points at
    private static PropertyType BuildAssetType(FAssetArchive Ar, string type, FPackageIndex? valueTypeObject)
    {
        var name = valueTypeObject?.Name;
        var propertyType = type == "EnumProperty"
            ? new PropertyType(type, enumName: name)
            : new PropertyType(type, name);

        var mappings = Ar.Owner?.Mappings;
        if (valueTypeObject is null || (type == "EnumProperty" ? mappings?.Enums.ContainsKey(valueTypeObject.Name) : mappings?.Types.ContainsKey(valueTypeObject.Name)) == true)
            return propertyType;

        if (type == "EnumProperty") propertyType.Enum = valueTypeObject.TryLoad<UEnum>(out var enumObject) ? enumObject : null;
        else propertyType.Struct = valueTypeObject.TryLoad<UStruct>(out var structObject) ? structObject : null;

        return propertyType;
    }

    // Enums are read with a byte underlying type. UE 5.8 uses the real one for enum classes, which only matters
    // for enum classes that aren't uint8, and those can't be used by Blueprints.
    private static PropertyType BuildPropertyType(FAssetArchive Ar, EPropertyBagPropertyType valueType, FPackageIndex? valueTypeObject) => valueType switch
    {
        EPropertyBagPropertyType.Bool => new PropertyType("BoolProperty", b: false),
        EPropertyBagPropertyType.Byte => new PropertyType("ByteProperty"),
        EPropertyBagPropertyType.Int8 => new PropertyType("Int8Property"),
        EPropertyBagPropertyType.Int16 => new PropertyType("Int16Property"),
        EPropertyBagPropertyType.UInt16 => new PropertyType("UInt16Property"),
        EPropertyBagPropertyType.Int32 => new PropertyType("IntProperty"),
        EPropertyBagPropertyType.Int64 => new PropertyType("Int64Property"),
        EPropertyBagPropertyType.UInt32 => new PropertyType("UInt32Property"),
        EPropertyBagPropertyType.UInt64 => new PropertyType("UInt64Property"),
        EPropertyBagPropertyType.Float => new PropertyType("FloatProperty"),
        EPropertyBagPropertyType.Double => new PropertyType("DoubleProperty"),
        EPropertyBagPropertyType.Name => new PropertyType("NameProperty"),
        EPropertyBagPropertyType.String => new PropertyType("StrProperty"),
        EPropertyBagPropertyType.Text => new PropertyType("TextProperty"),
        EPropertyBagPropertyType.Enum => BuildAssetType(Ar, "EnumProperty", valueTypeObject),
        EPropertyBagPropertyType.Struct => BuildAssetType(Ar, "StructProperty", valueTypeObject),
        EPropertyBagPropertyType.Object or EPropertyBagPropertyType.Class => new PropertyType("ObjectProperty"),
        EPropertyBagPropertyType.SoftObject => new PropertyType("SoftObjectProperty"),
        EPropertyBagPropertyType.SoftClass => new PropertyType("SoftClassProperty"),
        _ => new PropertyType("NoneProperty")
    };

    public enum EVersion : byte
    {
        InitialVersion = 0,
        SerializeStructSize,
        // -----<new versions can be added above this line>-----
        VersionPlusOne,
        LatestVersion = VersionPlusOne - 1
    };
}

public struct FPropertyBagPropertyDesc
{
    public FPackageIndex ValueTypeObject;
    public FGuid ID;
    public FName Name;
    public EPropertyBagPropertyType ValueType;
    public EPropertyBagContainerType ContainerType;
    public EPropertyFlags PropertyFlags;
    public FPropertyBagContainerTypes ContainerTypes;
    public FPropertyBagPropertyDescMetaData[]? MetaData;
    public FPackageIndex? MetaClass;
    public EPropertyBagPropertyType KeyType;
    public FPackageIndex? KeyTypeObject;

    public FPropertyBagPropertyDesc(FAssetArchive Ar)
    {
        ValueTypeObject = new FPackageIndex(Ar);
        ID = Ar.Read<FGuid>();
        Name = Ar.ReadFName();
        ValueType = ReadPropertyType(Ar);

        // Containers were added as a single type and became a list with nested containers
        var version = FPropertyBagCustomVersion.Get(Ar);
        if (version >= FPropertyBagCustomVersion.Type.NestedContainerTypes)
        {
            ContainerTypes = new FPropertyBagContainerTypes(Ar);
        }
        else if (version >= FPropertyBagCustomVersion.Type.ContainerTypes)
        {
            ContainerType = Ar.Read<EPropertyBagContainerType>();
            if (ContainerType != EPropertyBagContainerType.None)
                ContainerTypes = new FPropertyBagContainerTypes([ContainerType]);
        }

        var bHasMetaData = Ar.ReadBoolean();
        if (bHasMetaData)
        {
            MetaData = Ar.ReadArray(() => new FPropertyBagPropertyDescMetaData(Ar));
            if (FPropertyBagCustomVersion.Get(Ar) >= FPropertyBagCustomVersion.Type.MetaClass)
            {
                MetaClass = new FPackageIndex(Ar);
            }
        }

        if (FPropertyBagCustomVersion.Get(Ar) >= FPropertyBagCustomVersion.Type.PropertyFlags)
        {
            PropertyFlags = Ar.Read<EPropertyFlags>();
        }

        if (FPropertyBagCustomVersion.Get(Ar) >= FPropertyBagCustomVersion.Type.KeyTypes)
        {
            KeyType = ReadPropertyType(Ar);
            KeyTypeObject = new FPackageIndex(Ar);
        }
    }

    // UE 5.8 inserted Int8, Int16 and UInt16 before UInt32 without bumping a version
    private static EPropertyBagPropertyType ReadPropertyType(FAssetArchive Ar)
    {
        var type = Ar.Read<EPropertyBagPropertyType>();
        if (Ar.Game >= EGame.GAME_UE5_8)
            return type;

        return type switch
        {
            EPropertyBagPropertyType.Int8 => EPropertyBagPropertyType.UInt32,
            EPropertyBagPropertyType.Int16 => EPropertyBagPropertyType.UInt64,
            _ => type
        };
    }
}

public struct FPropertyBagPropertyDescMetaData(FAssetArchive Ar)
{
    public FName Key = Ar.ReadFName();
    public string Value = Ar.ReadFString();
}

public struct FPropertyBagContainerTypes
{
    public EPropertyBagContainerType[] Types;

    public FPropertyBagContainerTypes(EPropertyBagContainerType[] types)
    {
        Types = types;
    }

    public FPropertyBagContainerTypes(FAssetArchive Ar)
    {
        Types = Ar.ReadArray(Ar.Read<byte>(), Ar.Read<EPropertyBagContainerType>);
    }
}
