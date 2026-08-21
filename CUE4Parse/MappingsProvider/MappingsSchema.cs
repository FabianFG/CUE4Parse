using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.MappingsProvider;

public class Struct
{
    private Func<Struct?>? _superFactory;
    private Action? _changed;
    private string _name;
    private string? _superType;
    private int _propertyCount;
    public TypeMappings? Context { get; private set; }
    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value;
            _changed?.Invoke();
        }
    }
    public string? SuperType
    {
        get => _superType;
        set
        {
            if (_superType == value) return;
            _superType = value;
            ResetSuperResolution();
            _changed?.Invoke();
        }
    }
    public Lazy<Struct?> Super { get; protected set; }
    public TrackedDictionary<int, PropertyInfo> Properties { get; }
    public int PropertyCount
    {
        get => _propertyCount;
        set
        {
            if (_propertyCount == value) return;
            _propertyCount = value;
            _changed?.Invoke();
        }
    }

    public Struct(TypeMappings? context, string name, int propertyCount)
    {
        Context = context;
        _name = name;
        _propertyCount = propertyCount;
        if (context != null)
            _changed = context.Invalidate;
        Properties = new TrackedDictionary<int, PropertyInfo>(null, NotifyChanged,
            property => property.AttachChangeTracker(NotifyChanged));
        Super = new Lazy<Struct?>(() => null);
    }

    public Struct(TypeMappings? context, string name, string? superType, Dictionary<int, PropertyInfo> properties, int propertyCount) : this(context, name, propertyCount)
    {
        _superType = superType;
        _superFactory = () =>
        {
            if (SuperType != null && Context != null &&
                Context.TryGetType(TypeMappings.GetShortTypeName(SuperType), SuperType, out var superStruct))
            {
                return superStruct;
            }

            return null;
        };
        ResetSuperResolution();
        foreach (var (index, property) in properties)
            Properties.Add(index, property);
    }

    internal void AttachChangeTracker(TypeMappings context)
    {
        Context = context;
        _changed = context.Invalidate;
        foreach (var property in Properties.Values)
            property.AttachChangeTracker(NotifyChanged);
    }

    private void NotifyChanged() => _changed?.Invoke();

    internal void ResetSuperResolution()
    {
        if (_superFactory != null)
            Super = new Lazy<Struct?>(_superFactory);
    }

    public bool TryGetValue(int i, out PropertyInfo info)
    {
        if (!Properties.TryGetValue(i, out info))
        {
            return i >= PropertyCount && Super.Value != null &&
                   Super.Value.TryGetValue(i - PropertyCount, out info);
        }

        return true;
    }

    public bool TryGetValue(string propertyName, int arrayIndex, out PropertyInfo info)
    {
        PropertyInfo? matchingName = null;
        foreach (var property in Properties.Values)
        {
            if (!property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                continue;
            if (property.Index == arrayIndex &&
                property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                info = property;
                return true;
            }
            if (property.ArraySize is > 0 && arrayIndex < property.ArraySize)
                matchingName = property;
        }

        if (matchingName != null)
        {
            info = matchingName;
            return true;
        }

        if (Super.Value != null)
            return Super.Value.TryGetValue(propertyName, arrayIndex, out info);

        info = null!;
        return false;
    }

    public int CountProperties(bool includeSuper)
    {
        int total = 0;
        var current = this;

        while (current != null)
        {
            total += current.PropertyCount;
            current = includeSuper ? current.Super.Value : null;
        }

        return total;
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
                    if (Context != null && Context.TryGetType(
                            superStruct.Name,
                            struc.SuperStruct.ResolvedObject?.GetPathName() ??
                            (superStruct as UScriptClass)?.FullTypeIdentifier ??
                            superStruct.GetPathName(),
                            out var scriptStruct))
                    {
                        return scriptStruct;
                    }

                    Log.Warning("Missing prop mappings for type {0}", superStruct.Name);
                    return null;
                }

                return new SerializedStruct(Context, superStruct);
            }

            return null;
        });
        for (var i = 0; i < struc.ChildProperties.Length; i++)
        {
            var prop = (FProperty) struc.ChildProperties[i];
            var propInfo = new PropertyInfo(Math.Min(i, prop.ArrayDim - 1), prop.Name.Text, new PropertyType(prop), prop.ArrayDim);
            for (var j = 0; j < prop.ArrayDim; j++)
            {
                Properties[i + j] = propInfo;
            }
        }
    }
}

public class PropertyInfo : ICloneable
{
    private Action? _changed;
    private int _index;
    private string _name;
    private int? _arraySize;
    private PropertyType _mappingType;
    public int Index { get => _index; set { if (_index != value) { _index = value; _changed?.Invoke(); } } }
    public string Name { get => _name; set { if (_name != value) { _name = value; _changed?.Invoke(); } } }
    public int? ArraySize { get => _arraySize; set { if (_arraySize != value) { _arraySize = value; _changed?.Invoke(); } } }
    public PropertyType MappingType
    {
        get => _mappingType;
        set
        {
            if (ReferenceEquals(_mappingType, value)) return;
            _mappingType = value;
            _mappingType.AttachChangeTracker(NotifyChanged);
            _changed?.Invoke();
        }
    }

    public PropertyInfo(int index, string name, PropertyType mappingType, int? arraySize = null)
    {
        _index = index;
        _name = name;
        _arraySize = arraySize;
        _mappingType = mappingType;
    }

    internal void AttachChangeTracker(Action changed)
    {
        _changed = changed;
        _mappingType.AttachChangeTracker(NotifyChanged);
    }

    private void NotifyChanged() => _changed?.Invoke();

    public override string ToString() => $"{Index + 1}/{ArraySize} -> {Name}";
    public object Clone() => new PropertyInfo(Index, Name, MappingType, ArraySize);
}

public class PropertyType
{
    private Action? _changed;
    private string _type;
    private string? _structType;
    private PropertyType? _innerType;
    private PropertyType? _valueType;
    private string? _enumName;
    private bool? _isEnumAsByte;
    private bool? _bool;
    public string Type { get => _type; set => Set(ref _type, value); }
    public string? StructType { get => _structType; set => Set(ref _structType, value); }
    public PropertyType? InnerType { get => _innerType; set => SetNested(ref _innerType, value); }
    public PropertyType? ValueType { get => _valueType; set => SetNested(ref _valueType, value); }
    public string? EnumName { get => _enumName; set => Set(ref _enumName, value); }
    public bool? IsEnumAsByte { get => _isEnumAsByte; set => Set(ref _isEnumAsByte, value); }
    public bool? Bool { get => _bool; set => Set(ref _bool, value); }
    public UStruct? Struct;
    public UEnum? Enum;

    public PropertyType(string type, string? structType = null, PropertyType? innerType = null, PropertyType? valueType = null, string? enumName = null, bool? isEnumAsByte = null, bool? b = null)
    {
        _type = type;
        _structType = structType;
        _innerType = innerType;
        _valueType = valueType;
        _enumName = enumName;
        _isEnumAsByte = isEnumAsByte;
        _bool = b;
    }

    public PropertyType(FProperty prop)
    {
        _type = prop.GetType().Name[1..];
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
                StructType = structObj?.GetPathName();
                break;
            case FOptionalProperty optional:
                value = optional.ValueProperty;
                if (value != null) InnerType = new PropertyType(value);
                break;
        }
    }

    internal void AttachChangeTracker(Action changed)
    {
        _changed = changed;
        _innerType?.AttachChangeTracker(NotifyChanged);
        _valueType?.AttachChangeTracker(NotifyChanged);
    }

    private void NotifyChanged() => _changed?.Invoke();

    private void Set<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        _changed?.Invoke();
    }

    private void SetNested(ref PropertyType? field, PropertyType? value)
    {
        if (ReferenceEquals(field, value)) return;
        field = value;
        field?.AttachChangeTracker(NotifyChanged);
        _changed?.Invoke();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyEnum(FProperty prop, FPackageIndex enumIndex)
    {
        var enumObj = enumIndex.ResolvedObject;
        Enum = enumObj?.Object?.Value as UEnum;
        EnumName = enumObj?.GetPathName();
        InnerType = prop.ElementSize switch
        {
            4 => new PropertyType("IntProperty"),
            _ => null
        };
    }
}
