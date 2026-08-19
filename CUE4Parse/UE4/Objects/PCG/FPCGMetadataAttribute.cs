using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Properties;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.PCG;

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
        ValueType = fallback.GetOrDefault(nameof(ValueType), EPCGMetadataTypes.Unknown);
        ContainerTypes = fallback.GetOrDefault<EPCGMetadataAttributeContainerTypes[]>(nameof(ContainerTypes), []);
        ValueTypeObject = fallback.GetOrDefault<FPackageIndex?>(nameof(ValueTypeObject));
        KeyType = fallback.GetOrDefault(nameof(KeyType), EPCGMetadataTypes.Unknown);
        KeyTypeObject = fallback.GetOrDefault<FPackageIndex?>(nameof(KeyTypeObject));
    }

    internal FPropertyTagType ReadValue(FAssetArchive Ar)
    {
        var (propertyType, tagData) = CreatePropertyTagData();
        return FPropertyTagType.ReadPropertyTagType(Ar, propertyType, tagData, ReadType.RAW)
               ?? throw new ParserException(Ar, $"Unable to read PCG metadata value of type {ValueType}");
    }

    private (string Type, FPropertyTagData Data) CreatePropertyTagData()
    {
        var value = CreateScalarPropertyTagData(ValueType, ValueTypeObject);
        for (var i = ContainerTypes.Length - 1; i >= 0; i--)
        {
            value = ContainerTypes[i] switch
            {
                EPCGMetadataAttributeContainerTypes.None => value,
                EPCGMetadataAttributeContainerTypes.Array => ("ArrayProperty", new FPropertyTagData
                {
                    Type = "ArrayProperty", InnerType = value.Type, InnerTypeData = value.Data
                }),
                EPCGMetadataAttributeContainerTypes.Set => ("SetProperty", new FPropertyTagData
                {
                    Type = "SetProperty", InnerType = value.Type, InnerTypeData = value.Data
                }),
                EPCGMetadataAttributeContainerTypes.Map => CreateMapPropertyTagData(value),
                _ => throw new ParserException($"Unknown PCG metadata container type {ContainerTypes[i]}")
            };
        }

        return value;
    }

    private (string Type, FPropertyTagData Data) CreateMapPropertyTagData((string Type, FPropertyTagData Data) value)
    {
        var key = CreateScalarPropertyTagData(KeyType, KeyTypeObject);
        return ("MapProperty", new FPropertyTagData
        {
            Type = "MapProperty",
            InnerType = key.Type,
            InnerTypeData = key.Data,
            ValueType = value.Type,
            ValueTypeData = value.Data
        });
    }

    private static (string Type, FPropertyTagData Data) CreateScalarPropertyTagData(
        EPCGMetadataTypes valueType, FPackageIndex? typeObject)
    {
        var propertyType = valueType switch
        {
            EPCGMetadataTypes.Float => "FloatProperty",
            EPCGMetadataTypes.Double => "DoubleProperty",
            EPCGMetadataTypes.Integer32 => "IntProperty",
            EPCGMetadataTypes.Integer64 => "Int64Property",
            EPCGMetadataTypes.Vector2 or EPCGMetadataTypes.Vector or EPCGMetadataTypes.Vector4 or
                EPCGMetadataTypes.Quaternion or EPCGMetadataTypes.Transform or EPCGMetadataTypes.Rotator or
                EPCGMetadataTypes.SoftObjectPath or EPCGMetadataTypes.SoftClassPath or EPCGMetadataTypes.Struct => "StructProperty",
            EPCGMetadataTypes.String => "StrProperty",
            EPCGMetadataTypes.Boolean => "BoolProperty",
            EPCGMetadataTypes.Name => "NameProperty",
            EPCGMetadataTypes.Byte => "ByteProperty",
            EPCGMetadataTypes.Text => "TextProperty",
            EPCGMetadataTypes.Enum => "EnumProperty",
            EPCGMetadataTypes.Object or EPCGMetadataTypes.Class => "ObjectProperty",
            EPCGMetadataTypes.SoftObject or EPCGMetadataTypes.SoftClass => "SoftObjectProperty",
            _ => throw new ParserException($"Unsupported PCG metadata value type {valueType}")
        };

        var data = new FPropertyTagData { Type = propertyType };
        if (propertyType == "StructProperty")
        {
            data.StructType = valueType switch
            {
                EPCGMetadataTypes.Vector2 => "Vector2D",
                EPCGMetadataTypes.Vector => "Vector",
                EPCGMetadataTypes.Vector4 => "Vector4",
                EPCGMetadataTypes.Quaternion => "Quat",
                EPCGMetadataTypes.Transform => "Transform",
                EPCGMetadataTypes.Rotator => "Rotator",
                EPCGMetadataTypes.SoftObjectPath => "SoftObjectPath",
                EPCGMetadataTypes.SoftClassPath => "SoftClassPath",
                _ => typeObject?.Name
            };
        }
        else if (propertyType == "EnumProperty")
        {
            data.EnumName = typeObject?.Name;
            data.InnerType = "ByteProperty";
            data.InnerTypeData = new FPropertyTagData { Type = "ByteProperty" };
        }

        return (propertyType, data);
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
            : new FPCGMetadataAttributeDesc((EPCGMetadataTypes) Ar.Read<int>(), attributeName);
        var header = new FPCGMetadataAttributeHeader(Ar, descriptor, usesGenericValueLayout);

        FPCGMetadataAttributeBase attribute = header.Descriptor.ContainerTypes.Length == 0
            ? ReadScalarAttribute(Ar, header, usesGenericValueLayout)
            : new FPCGMetadataGenericAttribute(Ar, header);

        if (!usesGenericValueLayout && descriptor.ValueType == EPCGMetadataTypes.Boolean)
            Ar.Position += 3;
        return attribute;
    }

    private static FPCGMetadataAttributeBase ReadScalarAttribute(
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
            EPCGMetadataTypes.Transform => new FPCGMetadataAttribute<FTransform>(Ar, header, () => new FTransform(Ar), usesGenericLayout),
            EPCGMetadataTypes.String => new FPCGMetadataAttribute<string>(Ar, header, Ar.ReadFString, usesGenericLayout),
            EPCGMetadataTypes.Boolean => new FPCGMetadataAttribute<bool>(Ar, header, Ar.ReadFlag, usesGenericLayout),
            EPCGMetadataTypes.Rotator => new FPCGMetadataAttribute<FRotator>(Ar, header, () => new FRotator(Ar), usesGenericLayout),
            EPCGMetadataTypes.Name => new FPCGMetadataAttribute<FName>(Ar, header, Ar.ReadFName, usesGenericLayout),
            EPCGMetadataTypes.SoftObjectPath or EPCGMetadataTypes.SoftClassPath =>
                new FPCGMetadataAttribute<FSoftObjectPath>(Ar, header, () => new FSoftObjectPath(Ar), usesGenericLayout),
            EPCGMetadataTypes.Byte or EPCGMetadataTypes.Enum =>
                new FPCGMetadataAttribute<byte>(Ar, header, Ar.Read<byte>, usesGenericLayout),
            EPCGMetadataTypes.Text => new FPCGMetadataAttribute<FText>(Ar, header, () => new FText(Ar), usesGenericLayout),
            _ when usesGenericLayout => new FPCGMetadataGenericAttribute(Ar, header),
            _ => throw new ParserException(Ar, $"Unknown EPCGMetadataTypes value {header.Descriptor.ValueType}")
        };
    }
}

public class FPCGMetadataAttribute<T> : FPCGMetadataAttributeBase
{
    [JsonIgnore]
    public T[] Values = [];
    public T DefaultValue = default!;

    public FPCGMetadataAttribute(FAssetArchive Ar, Func<T> getter) : base(Ar)
    {
        Values = Ar.ReadArray(getter);
        DefaultValue = getter();
    }

    internal FPCGMetadataAttribute(FAssetArchive Ar, FPCGMetadataAttributeHeader header, Func<T> getter,
        bool usesGenericLayout) : base(header)
    {
        if (usesGenericLayout)
        {
            DefaultValue = getter();
            Values = Ar.ReadArray(getter);
        }
        else
        {
            Values = Ar.ReadArray(getter);
            DefaultValue = getter();
        }
    }
}

public class FPCGMetadataGenericAttribute : FPCGMetadataAttributeBase
{
    public FPropertyTagType DefaultValue;
    [JsonIgnore]
    public FPropertyTagType[] Values;

    internal FPCGMetadataGenericAttribute(FAssetArchive Ar, FPCGMetadataAttributeHeader header) : base(header)
    {
        DefaultValue = Descriptor.ReadValue(Ar);
        Values = Ar.ReadArray(() => Descriptor.ReadValue(Ar));
    }
}
