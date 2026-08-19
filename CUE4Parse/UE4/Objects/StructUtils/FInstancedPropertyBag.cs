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
    UInt32,	// Type not fully supported at UI, will work with restrictions to type editing
    UInt64, // Type not fully supported at UI, will work with restrictions to type editing

    Count
};

[JsonConverter(typeof(EnumConverter<EPropertyBagContainerType>))]
public enum EPropertyBagContainerType : byte
{
    None,
    Array,
    Count
};

public class FInstancedPropertyBag : IUStruct
{
    public FPropertyBagPropertyDesc[] PropertyDescs = [];
    public int SerialSize;
    public List<FPropertyTag> Properties = [];

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

        // The struct isn't serialized - GetOrCreateFromDescs rebuilds it, so the descs are the schema
        var payloadStart = Ar.Position;
        try
        {
            if (Ar.HasUnversionedProperties)
                Assets.Exports.UObject.DeserializePropertiesUnversioned(Properties, Ar, BuildSchema(Ar), nameof(FInstancedPropertyBag));
            else
                Assets.Exports.UObject.DeserializePropertiesTagged(Properties, Ar, false);
        }
        catch (Exception e)
        {
            // An unresolvable desc makes the whole payload unreadable, which is what SerialSize is for
            Properties.Clear();
            Log.Warning(e, "Failed to read FInstancedPropertyBag values, skipping {Size} bytes", SerialSize);
        }

        if (SerialSize > 0) Ar.Position = payloadStart + SerialSize;
    }

    // Descriptors in order are the schema, each bag type mapped onto its reflected property
    private Struct BuildSchema(FAssetArchive Ar)
    {
        var properties = new Dictionary<int, PropertyInfo>();
        for (var i = 0; i < PropertyDescs.Length; i++)
        {
            var desc = PropertyDescs[i];
            var propertyType = BuildPropertyType(Ar, desc.ValueType, desc.ValueTypeObject);

            // Containers wrap the value type, outermost last
            var containers = desc.ContainerTypes.Types ?? (desc.ContainerType != EPropertyBagContainerType.None ? [desc.ContainerType] : []);
            for (var c = containers.Length - 1; c >= 0; c--)
            {
                if (containers[c] == EPropertyBagContainerType.Array)
                    propertyType = new PropertyType("ArrayProperty", innerType: propertyType);
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

        if (name is null || Ar.Owner?.Mappings?.Types.ContainsKey(name) == true) return propertyType;

        if (type == "EnumProperty") propertyType.Enum = valueTypeObject?.Load<UEnum>();
        else propertyType.Struct = valueTypeObject?.Load<UStruct>();

        return propertyType;
    }

    private static PropertyType BuildPropertyType(FAssetArchive Ar, EPropertyBagPropertyType valueType, FPackageIndex? valueTypeObject) => valueType switch
    {
        EPropertyBagPropertyType.Bool => new PropertyType("BoolProperty", b: false),
        EPropertyBagPropertyType.Byte => new PropertyType("ByteProperty"),
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
        ValueType = Ar.Read<EPropertyBagPropertyType>();

        if (FPropertyBagCustomVersion.Get(Ar) < FPropertyBagCustomVersion.Type.ContainerTypes)
        {
            ContainerType = Ar.Read<EPropertyBagContainerType>();
            if (ContainerType != EPropertyBagContainerType.None)
                ContainerTypes = new FPropertyBagContainerTypes([ContainerType]);
        }
        else
        {
            ContainerTypes = new FPropertyBagContainerTypes(Ar);
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
            KeyType = Ar.Read<EPropertyBagPropertyType>();
            KeyTypeObject = new FPackageIndex(Ar);
        }
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
