using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CUE4Parse.UE4.Objects.PCG;

[JsonConverter(typeof(StringEnumConverter))]
public enum EPCGMetadataTypes : byte
{
    Float = 0,
    Double,
    Integer32,
    Integer64,
    Vector2,
    Vector,
    Vector4,
    Quaternion,
    Transform,
    String,
    Boolean,
    Rotator,
    Name,
    SoftObjectPath,
    SoftClassPath,

    EndLegacyTypes,
    Byte,
    Text,
    Enum,
    Struct,
    Object,
    SoftObject,
    Class,
    SoftClass,

    Count,
    Unknown = 255,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum EPCGMetadataAttributeContainerTypes
{
    None,
    Array,
    Set,
    Map
}

public class FPCGMetadataAttributeDesc
{
    public FName Name;
    public EPCGMetadataTypes ValueType = EPCGMetadataTypes.Unknown;
    public EPCGMetadataAttributeContainerTypes[] ContainerTypes = [];
    public FPackageIndex? ValueTypeObject;
    public EPCGMetadataTypes KeyType = EPCGMetadataTypes.Unknown;
    public FPackageIndex? KeyTypeObject;

    public FPCGMetadataAttributeDesc(EPCGMetadataTypes valueType, FName name)
    {
        Name = name;
        ValueType = valueType;
    }

    public FPCGMetadataAttributeDesc(FAssetArchive Ar, FName fallbackName)
    {
        var fallback = new FStructFallback(Ar, "PCGMetadataAttributeDesc");
        Name = fallback.GetOrDefault(nameof(Name), fallbackName);
        ValueType = ReadEnum(fallback, nameof(ValueType), EPCGMetadataTypes.Unknown);
        ContainerTypes = ReadContainerTypes(fallback);
        ValueTypeObject = fallback.GetOrDefault<FPackageIndex?>(nameof(ValueTypeObject));
        KeyType = ReadEnum(fallback, nameof(KeyType), EPCGMetadataTypes.Unknown);
        KeyTypeObject = fallback.GetOrDefault<FPackageIndex?>(nameof(KeyTypeObject));
    }

    // A missing property keeps its default. A value that is there but doesn't convert (an enum value the mappings don't
    // know) can't fall back to the default: the descriptor decides how much UE wrote after it
    private static TEnum ReadEnum<TEnum>(FStructFallback fallback, string name, TEnum defaultValue) where TEnum : struct, Enum
    {
        if (fallback.Properties.FirstOrDefault(property => property.Name.Text == name)?.Tag is not { } tag)
            return defaultValue;
        return tag.GetValue(typeof(TEnum)) is TEnum value
            ? value
            : throw new ParserException($"PCG metadata attribute descriptor has a {name} of {tag} that isn't a known {typeof(TEnum).Name}");
    }

    private static EPCGMetadataAttributeContainerTypes[] ReadContainerTypes(FStructFallback fallback)
    {
        if (fallback.Properties.FirstOrDefault(property => property.Name.Text == nameof(ContainerTypes))?.Tag is not ArrayProperty { Value: { } array })
            return [];

        var containers = new EPCGMetadataAttributeContainerTypes[array.Properties.Count];
        for (var i = 0; i < containers.Length; i++)
        {
            containers[i] = array.Properties[i].GetValue(typeof(EPCGMetadataAttributeContainerTypes)) is EPCGMetadataAttributeContainerTypes container
                ? container
                : throw new ParserException($"PCG metadata attribute descriptor has a container type of {array.Properties[i]} that isn't a known EPCGMetadataAttributeContainerTypes");
        }
        return containers;
    }

    // UE treats an attribute as a single value when its outer container is None
    internal bool IsSingleValue => ContainerTypes.Length == 0 || ContainerTypes[0] == EPCGMetadataAttributeContainerTypes.None;

    // Describes the property UE builds for this descriptor, or null when it can't build one and serializes no values
    internal FPropertyTagData? CreatePropertyTagData()
    {
        var value = CreateScalarPropertyTagData(ValueType, ValueTypeObject);
        for (var i = ContainerTypes.Length - 1; i >= 0 && value != null; i--)
        {
            value = ContainerTypes[i] switch
            {
                EPCGMetadataAttributeContainerTypes.Array => new FPropertyTagData
                {
                    Type = "ArrayProperty", InnerType = value.Type, InnerTypeData = value
                },
                EPCGMetadataAttributeContainerTypes.Set => new FPropertyTagData
                {
                    Type = "SetProperty", InnerType = value.Type, InnerTypeData = value
                },
                EPCGMetadataAttributeContainerTypes.Map => CreateScalarPropertyTagData(KeyType, KeyTypeObject) is { } key
                    ? new FPropertyTagData
                    {
                        Type = "MapProperty", InnerType = key.Type, InnerTypeData = key, ValueType = value.Type, ValueTypeData = value
                    }
                    : null,
                // None and unknown containers add no layer, like in UE
                _ => value
            };
        }

        return value;
    }

    private static FPropertyTagData? CreateScalarPropertyTagData(EPCGMetadataTypes valueType, FPackageIndex? typeObject)
    {
        if (valueType is EPCGMetadataTypes.Enum or EPCGMetadataTypes.Struct)
        {
            if (typeObject is not { IsNull: false })
                return null;
            if (valueType == EPCGMetadataTypes.Enum)
                return new FPropertyTagData { Type = "EnumProperty", EnumName = typeObject.Name };

            // Keep the loaded struct, user-defined structs aren't part of the mappings. UE still wrote the values when
            // the struct can't be loaded here, so unversioned packages fail on them instead of skipping them.
            typeObject.TryLoad<UStruct>(out var struc);
            return new FPropertyTagData { Type = "StructProperty", StructType = typeObject.Name, Struct = struc };
        }

        if (valueType is EPCGMetadataTypes.Object or EPCGMetadataTypes.SoftObject or EPCGMetadataTypes.Class or EPCGMetadataTypes.SoftClass)
            throw new ParserException($"UE can't load PCG metadata attributes of type {valueType}");

        var structType = valueType switch
        {
            EPCGMetadataTypes.Vector2 => "Vector2D",
            EPCGMetadataTypes.Vector => "Vector",
            EPCGMetadataTypes.Vector4 => "Vector4",
            EPCGMetadataTypes.Quaternion => "Quat",
            EPCGMetadataTypes.Transform => "Transform",
            EPCGMetadataTypes.Rotator => "Rotator",
            EPCGMetadataTypes.SoftObjectPath => "SoftObjectPath",
            EPCGMetadataTypes.SoftClassPath => "SoftClassPath",
            _ => null
        };
        if (structType != null)
            return new FPropertyTagData { Type = "StructProperty", StructType = structType };

        var propertyType = valueType switch
        {
            EPCGMetadataTypes.Float => "FloatProperty",
            EPCGMetadataTypes.Double => "DoubleProperty",
            EPCGMetadataTypes.Integer32 => "IntProperty",
            EPCGMetadataTypes.Integer64 => "Int64Property",
            EPCGMetadataTypes.String => "StrProperty",
            EPCGMetadataTypes.Boolean => "BoolProperty",
            EPCGMetadataTypes.Name => "NameProperty",
            EPCGMetadataTypes.Byte => "ByteProperty",
            EPCGMetadataTypes.Text => "TextProperty",
            _ => null
        };
        return propertyType != null ? new FPropertyTagData { Type = propertyType } : null;
    }
}

internal sealed class FPCGMetadataAttributeHeader
{
    public readonly Dictionary<long, int> EntryToValueKeyMap;
    public readonly int ParentAttributeId;
    public readonly FName Name;
    public readonly int AttributeId;
    public readonly FPCGMetadataAttributeDesc Descriptor;

    public FPCGMetadataAttributeHeader(FAssetArchive Ar, FPCGMetadataAttributeDesc descriptor, bool usesGenericValueLayout)
    {
        EntryToValueKeyMap = Ar.ReadMap(Ar.Read<long>, Ar.Read<int>);
        ParentAttributeId = Ar.Read<int>();
        Name = Ar.ReadFName();
        AttributeId = Ar.Read<int>();
        Descriptor = usesGenericValueLayout && ParentAttributeId < 0
            ? new FPCGMetadataAttributeDesc(Ar, Name)
            : descriptor;
    }
}

public abstract class FPCGMetadataAttributeBase
{
    [JsonIgnore]
    public Dictionary<long, int> EntryToValueKeyMap;
    public int ParentAttributeId;
    public FName Name;
    public int AttributeId;
    public FPCGMetadataAttributeDesc Descriptor;

    [Obsolete("Only reads the layout from before UE 5.8. Use ReadPCGMetadataAttribute, which handles every version.")]
    public FPCGMetadataAttributeBase(FAssetArchive Ar)
    {
        EntryToValueKeyMap = Ar.ReadMap(Ar.Read<long>, Ar.Read<int>);
        ParentAttributeId = Ar.Read<int>();
        Name = Ar.ReadFName();
        AttributeId = Ar.Read<int>();
        Descriptor = new FPCGMetadataAttributeDesc(EPCGMetadataTypes.Unknown, Name);
    }

    private protected FPCGMetadataAttributeBase(FPCGMetadataAttributeHeader header)
    {
        EntryToValueKeyMap = header.EntryToValueKeyMap;
        ParentAttributeId = header.ParentAttributeId;
        Name = header.Name;
        AttributeId = header.AttributeId;
        Descriptor = header.Descriptor;
    }

    public static FPCGMetadataAttributeBase ReadPCGMetadataAttribute(FAssetArchive Ar) =>
        ReadPCGMetadataAttribute(Ar, default);

    public static FPCGMetadataAttributeBase ReadPCGMetadataAttribute(FAssetArchive Ar, FName attributeName)
    {
        var version = FFortniteMainBranchObjectVersion.Get(Ar);
        var usesDescriptorLayout = version >=
                                   FFortniteMainBranchObjectVersion.Type.MergePCGMetadataAttributeBaseAndGeneric;
        var usesGenericValueLayout = version >=
                                     FFortniteMainBranchObjectVersion.Type.ConvertFPCGMetadataAttributeToGenericAttributes;
        var descriptor = usesDescriptorLayout
            ? new FPCGMetadataAttributeDesc(Ar, attributeName)
            : new FPCGMetadataAttributeDesc(ReadLegacyType(Ar), attributeName);
        var header = new FPCGMetadataAttributeHeader(Ar, descriptor, usesGenericValueLayout);

        // Like UE, single values of the legacy types keep their typed attribute, everything else is generic
        var desc = header.Descriptor;
        if (desc.IsSingleValue && desc.ValueType < EPCGMetadataTypes.EndLegacyTypes)
            return ReadTypedAttribute(Ar, header, usesGenericValueLayout);
        if (usesGenericValueLayout)
            return new FPCGMetadataGenericAttribute(Ar, header);
        throw new ParserException(Ar, $"Unknown EPCGMetadataTypes value {desc.ValueType}");
    }

    // Before the descriptor, the domain wrote the type as an int32 and only knew the legacy types
    private static EPCGMetadataTypes ReadLegacyType(FAssetArchive Ar)
    {
        var type = Ar.Read<int>();
        if (type < 0 || type >= (int) EPCGMetadataTypes.EndLegacyTypes)
            throw new ParserException(Ar, $"Unknown EPCGMetadataTypes value {type}");
        return (EPCGMetadataTypes) type;
    }

    // Keeps its own switch instead of reading through CreatePropertyTagData because typed attributes expose their
    // element type (FPCGMetadataAttribute<float> and so on). PcgMetadataAttributeTests checks that both read the same bytes
    private static FPCGMetadataAttributeBase ReadTypedAttribute(
        FAssetArchive Ar, FPCGMetadataAttributeHeader header, bool usesGenericLayout)
    {
        return header.Descriptor.ValueType switch
        {
            EPCGMetadataTypes.Float => new FPCGMetadataAttribute<float>(Ar, header, Ar.Read<float>, usesGenericLayout),
            EPCGMetadataTypes.Double => new FPCGMetadataAttribute<double>(Ar, header, Ar.Read<double>, usesGenericLayout),
            EPCGMetadataTypes.Integer32 => new FPCGMetadataAttribute<int>(Ar, header, Ar.Read<int>, usesGenericLayout),
            EPCGMetadataTypes.Integer64 => new FPCGMetadataAttribute<long>(Ar, header, Ar.Read<long>, usesGenericLayout),
            EPCGMetadataTypes.Vector2 => new FPCGMetadataAttribute<FVector2D>(Ar, header, () => new FVector2D(Ar), usesGenericLayout),
            EPCGMetadataTypes.Vector => new FPCGMetadataAttribute<FVector>(Ar, header, () => new FVector(Ar), usesGenericLayout),
            EPCGMetadataTypes.Vector4 => new FPCGMetadataAttribute<FVector4>(Ar, header, () => new FVector4(Ar), usesGenericLayout),
            EPCGMetadataTypes.Quaternion => new FPCGMetadataAttribute<FQuat>(Ar, header, () => new FQuat(Ar), usesGenericLayout),
            // FTransform has no native serializer, so the generic layout stores it as properties
            EPCGMetadataTypes.Transform => new FPCGMetadataAttribute<FTransform>(Ar, header, usesGenericLayout
                ? () => new FTransform(new FStructFallback(Ar, "Transform"))
                : () => new FTransform(Ar), usesGenericLayout),
            EPCGMetadataTypes.String => new FPCGMetadataAttribute<string>(Ar, header, Ar.ReadFString, usesGenericLayout),
            // The legacy layout writes the bool values as bytes but the default value as a 32-bit bool
            EPCGMetadataTypes.Boolean => new FPCGMetadataAttribute<bool>(Ar, header, Ar.ReadFlag, usesGenericLayout,
                usesGenericLayout ? Ar.ReadFlag : Ar.ReadBoolean),
            EPCGMetadataTypes.Rotator => new FPCGMetadataAttribute<FRotator>(Ar, header, () => new FRotator(Ar), usesGenericLayout),
            EPCGMetadataTypes.Name => new FPCGMetadataAttribute<FName>(Ar, header, Ar.ReadFName, usesGenericLayout),
            EPCGMetadataTypes.SoftObjectPath or EPCGMetadataTypes.SoftClassPath =>
                new FPCGMetadataAttribute<FSoftObjectPath>(Ar, header, () => new FSoftObjectPath(Ar), usesGenericLayout),
            _ => throw new ParserException(Ar, $"Unknown EPCGMetadataTypes value {header.Descriptor.ValueType}")
        };
    }
}

public class FPCGMetadataAttribute<T> : FPCGMetadataAttributeBase
{
    [JsonIgnore]
    public T[] Values = [];
    public T DefaultValue = default!;

    [Obsolete("Only reads the layout from before UE 5.8. Use FPCGMetadataAttributeBase.ReadPCGMetadataAttribute, which handles every version.")]
    public FPCGMetadataAttribute(FAssetArchive Ar, Func<T> getter) : base(Ar)
    {
        Values = Ar.ReadArray(getter);
        DefaultValue = getter();
    }

    internal FPCGMetadataAttribute(FAssetArchive Ar, FPCGMetadataAttributeHeader header, Func<T> getter,
        bool usesGenericLayout, Func<T>? defaultGetter = null) : base(header)
    {
        defaultGetter ??= getter;
        if (usesGenericLayout)
        {
            DefaultValue = defaultGetter();
            Values = Ar.ReadArray(getter);
        }
        else
        {
            Values = Ar.ReadArray(getter);
            DefaultValue = defaultGetter();
        }
    }
}

public class FPCGMetadataGenericAttribute : FPCGMetadataAttributeBase
{
    public FPropertyTagType? DefaultValue;
    [JsonIgnore]
    public FPropertyTagType[] Values = [];

    internal FPCGMetadataGenericAttribute(FAssetArchive Ar, FPCGMetadataAttributeHeader header) : base(header)
    {
        if (Descriptor.CreatePropertyTagData() is not { } property)
            return;

        DefaultValue = ReadValue(Ar, property);
        Values = [.. ReadValues(Ar, property)];
    }

    // Same layout as FProperty::SerializeItem, which UE uses for the default value and every element of the values
    private static FPropertyTagType ReadValue(FAssetArchive Ar, FPropertyTagData property)
    {
        switch (property.Type)
        {
            case "ArrayProperty":
                return new ArrayProperty(new UScriptArray(ReadValues(Ar, property.InnerTypeData!), property.InnerType!, property.InnerTypeData));
            case "SetProperty":
                SkipRemovedValues(Ar, property.InnerTypeData!);
                return new SetProperty(new UScriptSet(ReadValues(Ar, property.InnerTypeData!)));
            case "MapProperty":
                SkipRemovedValues(Ar, property.InnerTypeData!);
                var count = ReadCount(Ar);
                var entries = new Dictionary<FPropertyTagType, FPropertyTagType?>(count);
                for (var i = 0; i < count; i++)
                {
                    var key = ReadValue(Ar, property.InnerTypeData!);
                    entries[key] = ReadValue(Ar, property.ValueTypeData!);
                }
                return new MapProperty(new UScriptMap(entries));
            default:
                return FPropertyTagType.ReadPropertyTagType(Ar, property.Type, property, ReadType.ARRAY)
                       ?? throw new ParserException(Ar, $"Unable to read PCG metadata value of type {property.Type}");
        }
    }

    private static List<FPropertyTagType> ReadValues(FAssetArchive Ar, FPropertyTagData element)
    {
        var count = ReadCount(Ar);
        var values = new List<FPropertyTagType>(count);
        for (var i = 0; i < count; i++)
            values.Add(ReadValue(Ar, element));
        return values;
    }

    // Sets and maps start with the elements to remove from their defaults, PCG saves them without defaults
    private static void SkipRemovedValues(FAssetArchive Ar, FPropertyTagData element)
    {
        var count = Ar.Read<int>();
        for (var i = 0; i < count; i++)
            ReadValue(Ar, element);
    }

    private static int ReadCount(FAssetArchive Ar)
    {
        var count = Ar.Read<int>();
        if (count < 0 || count > Ar.Length - Ar.Position)
            throw new ParserException(Ar, $"Invalid PCG metadata element count {count}");
        return count;
    }
}
