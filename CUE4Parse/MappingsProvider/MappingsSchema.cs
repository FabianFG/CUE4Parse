using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.MappingsProvider;

public class Struct
{
    public readonly TypeMappings? Context;
    public string Name;
    public string? SuperType;
    public Lazy<Struct?> Super = new((Struct?) null);
    public Dictionary<int, PropertyInfo> Properties = new();
    private readonly int _propCountClassFlag;
    public uint Flags;

    private Lazy<(Dictionary<int, PropertyInfo> Properties, int PropertyCount)>? _cookedSchema;

    public int PropertyCount => _propCountClassFlag & 0xFFFFFF;
    public bool IsClass => _propCountClassFlag >>> 24 == 0;

    public Struct(TypeMappings? context, string name, int propCountClassFlag, uint flags = 0)
    {
        _propCountClassFlag = propCountClassFlag;
        Context = context;
        Name = name;
        Flags = flags;
    }

    public Struct(TypeMappings? context, string name, string? superType, Dictionary<int, PropertyInfo> properties, int propCountClassFlag, uint flags = 0) : this(context, name, propCountClassFlag, flags)
    {
        SuperType = superType;
        Super = new Lazy<Struct?>(() =>
        {
            if (SuperType != null && Context != null && Context.Types.TryGetValue(SuperType, out var superStruct))
            {
                return superStruct;
            }

            return null;
        });
        Properties = properties;
    }

    public bool TryGetValue(int i, out PropertyInfo info) => TryGetValue(i, false, out info);

    public bool TryGetValue(int i, bool filterEditorOnly, out PropertyInfo info)
    {
        var (properties, propertyCount) = GetSchema(filterEditorOnly);
        if (!properties.TryGetValue(i, out info))
        {
            return i >= propertyCount && Super.Value != null &&
                   Super.Value.TryGetValue(i - propertyCount, filterEditorOnly, out info);
        }

        return true;
    }

    public int CountProperties(bool includeSuper, bool filterEditorOnly = false)
    {
        var total = 0;
        var current = this;

        while (current != null)
        {
            total += current.GetSchema(filterEditorOnly).PropertyCount;
            current = includeSuper ? current.Super.Value : null;
        }

        return total;
    }

    private (Dictionary<int, PropertyInfo> Properties, int PropertyCount) GetSchema(bool filterEditorOnly)
    {
        if (!filterEditorOnly)
            return (Properties, PropertyCount);

        var cooked = _cookedSchema;
        if (cooked == null)
        {
            cooked = new Lazy<(Dictionary<int, PropertyInfo>, int)>(BuildCookedSchema, LazyThreadSafetyMode.ExecutionAndPublication);
            var existing = Interlocked.CompareExchange(ref _cookedSchema, cooked, null);
            if (existing != null)
                cooked = existing;
        }

        return cooked.Value;
    }

    private (Dictionary<int, PropertyInfo> Properties, int PropertyCount) BuildCookedSchema()
    {
        var skipped = 0;
        for (var origIndex = 0; origIndex < PropertyCount; origIndex++)
        {
            if (Properties.TryGetValue(origIndex, out var info) && info.IsEditorOnly)
                skipped++;
        }

        if (skipped == 0)
            return (Properties, PropertyCount);

        var cooked = new Dictionary<int, PropertyInfo>(Math.Max(0, Properties.Count - skipped));
        skipped = 0;
        for (var origIndex = 0; origIndex < PropertyCount; origIndex++)
        {
            if (!Properties.TryGetValue(origIndex, out var info))
                continue;

            if (info.IsEditorOnly)
            {
                skipped++;
                continue;
            }

            cooked[origIndex - skipped] = info;
        }

        return (cooked, PropertyCount - skipped);
    }
}

public class SerializedStruct : Struct
{
    public SerializedStruct(TypeMappings? context, UStruct struc) : base(context, struc.Name, struc.ChildProperties.Length)
    {
        Super = new Lazy<Struct?>(() =>
        {
            //if (struc.SuperStruct.TryLoad<UStruct>(out var superStruct))
            var superStruct = struc.SuperStruct.Load<UStruct>();
            if (superStruct != null)
            {
                if (superStruct is UScriptClass)
                {
                    if (Context != null && Context.Types.TryGetValue(superStruct.Name, out var scriptStruct))
                    {
                        return scriptStruct;
                    }

                    Log.Warning("Missing prop mappings for type {SuperName}", superStruct.Name);
                    return null;
                }

                return new SerializedStruct(Context, superStruct);
            }

            return null;
        });
        Properties = new Dictionary<int, PropertyInfo>();
        for (var i = 0; i < struc.ChildProperties.Length; i++)
        {
            var prop = (FProperty) struc.ChildProperties[i];
            var propInfo = new PropertyInfo(Math.Min(i, prop.ArrayDim - 1), prop.Name.Text, new PropertyType(prop), prop.ArrayDim, prop.PropertyFlags);
            for (var j = 0; j < prop.ArrayDim; j++)
            {
                Properties[i + j] = propInfo;
            }
        }
    }
}

public class PropertyInfo(int index, string name, PropertyType mappingType, int? arraySize = null, EPropertyFlags propertyFlags = EPropertyFlags.None) : ICloneable
{
    public string Name = name;
    public int Index = index;
    public int ArraySize = arraySize ?? 1;
    public PropertyType MappingType = mappingType;
    public EPropertyFlags PropertyFlags = propertyFlags;

    public bool IsEditorOnly => PropertyFlags.HasFlag(EPropertyFlags.EditorOnly);

    public override string ToString() => $"{Index + 1}/{ArraySize} -> {Name}";
    public object Clone() => MemberwiseClone();
}

public class PropertyType
{
    public string Type;
    public string? StructType;
    public PropertyType? InnerType;
    public PropertyType? ValueType;
    public string? EnumName;
    public bool? IsEnumAsByte;
    public bool? Bool;
    public UStruct? Struct;
    public UEnum? Enum;

    public PropertyType(string type, string? structType = null, PropertyType? innerType = null, PropertyType? valueType = null, string? enumName = null, bool? isEnumAsByte = null, bool? b = null)
    {
        Type = type;
        StructType = structType;
        InnerType = innerType;
        ValueType = valueType;
        EnumName = enumName;
        IsEnumAsByte = isEnumAsByte;
        Bool = b;
    }

    public PropertyType(FProperty prop)
    {
        Type = prop.GetType().Name[1..];
        switch (prop)
        {
            case FArrayProperty array:
                var inner = array.Inner;
                if (inner != null) InnerType = new PropertyType(inner);
                break;
            case FByteProperty b:
                ApplyEnum(prop, b.Enum);
                break;
            case FEnumProperty e:
                ApplyEnum(prop, e.Enum);
                break;
            case FMapProperty map:
                var key = map.KeyProp;
                var value = map.ValueProp;
                if (key != null) InnerType = new PropertyType(key);
                if (value != null) ValueType = new PropertyType(value);
                break;
            case FSetProperty set:
                var element = set.ElementProp;
                if (element != null) InnerType = new PropertyType(element);
                break;
            case FStructProperty struc:
                var structObj = struc.Struct.ResolvedObject;
                Struct = structObj?.Object?.Value as UStruct;
                StructType = structObj?.Name.Text;
                break;
            case FOptionalProperty optional:
                value = optional.ValueProperty;
                if (value != null) InnerType = new PropertyType(value);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyEnum(FProperty prop, FPackageIndex enumIndex)
    {
        var enumObj = enumIndex.ResolvedObject;
        Enum = enumObj?.Object?.Value as UEnum;
        EnumName = enumObj?.Name.Text;
        InnerType = prop.ElementSize switch
        {
            4 => new PropertyType("IntProperty"),
            _ => null
        };
    }
}