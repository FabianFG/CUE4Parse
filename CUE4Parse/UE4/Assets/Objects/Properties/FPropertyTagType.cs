using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CUE4Parse.GameTypes.FN.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Objects.Properties;

public enum ReadType : byte
{
    ZERO,
    NORMAL,
    MAP,
    ARRAY
}

public abstract class FPropertyTagType<T> : FPropertyTagType
{
    public T? Value { get; protected set; }

    public override object? GenericValue => Value;

    public override string ToString() => Value != null ? $"{Value.ToString()} ({GetType().Name})" : string.Empty;
}

[JsonConverter(typeof(FPropertyTagTypeConverter))]
public abstract class FPropertyTagType
{
    public abstract object? GenericValue { get; }
    public object? GetValue(Type type)
    {
        var generic = GenericValue;
        if (type.IsInstanceOfType(generic))
        {
            return generic;
        }
        switch (this)
        {
            case FPropertyTagType<FScriptStruct> structProp when type.IsInstanceOfType(structProp.Value!.StructType):
                return structProp.Value.StructType;
            case FPropertyTagType<FScriptStruct> {Value.StructType: FStructFallback fallback} when type.GetCustomAttribute<StructFallback>() != null:
                return fallback.MapToClass(type);
            case FPropertyTagType<UScriptArray> arrayProp when type.IsArray:
            {
                var array = arrayProp.Value!.Properties;
                var contentType = type.GetElementType()!;
                var result = Array.CreateInstance(contentType, array.Count);
                for (var i = 0; i < array.Count; i++)
                {
                    result.SetValue(array[i].GetValue(contentType), i);
                }
                return result;
            }
            case FPropertyTagType<UScriptArray> arrayProp when typeof(IList).IsAssignableFrom(type):
            {
                var array = arrayProp.Value!.Properties;
                var contentType = type.GenericTypeArguments[0];
                var listType = typeof(List<>).MakeGenericType(contentType);
                var result = (IList) Activator.CreateInstance(listType, array.Count)!;
                foreach (var element in array)
                {
                    result.Add(element.GetValue(contentType));
                }
                return result;
            }
            case FPropertyTagType<FPackageIndex> objProp when typeof(UObject).IsAssignableFrom(type):
                if (objProp.Value!.TryLoad(out var objExport) && type.IsInstanceOfType(objExport))
                    return objExport;
                return null;
            case FPropertyTagType<FPackageIndex> objProp when typeof(ResolvedObject).IsAssignableFrom(type):
                return objProp.Value!.ResolvedObject;
            case FPropertyTagType<FSoftObjectPath> softObjProp when typeof(UObject).IsAssignableFrom(type):
                if (softObjProp.Value.TryLoad(out var softExport) && type.IsInstanceOfType(softExport))
                    return softExport;
                return null;
            case EnumProperty enumProp when type.IsEnum:
                var storedEnum = enumProp.Value.Text;
                var search = storedEnum.SubstringAfter("::"); // Strip enum name on namespaced and enum class enums
                var values = type.GetEnumNames();
                var idx = Array.FindIndex(values, it => it == search);
                return idx == -1 ? null : type.GetEnumValues().GetValue(idx);
            //TODO There are also Enums stored as ByteProperty but UModel uses them nowhere besides in UE2
            //TODO Maybe Maps?
            default:
                return null;
        }
    }

    public T? GetValue<T>()
    {
        return (T?) GetValue(typeof(T));
    }

    public abstract override string ToString();

    internal static FPropertyTagType? ReadPropertyTagType(FAssetArchive Ar, string propertyType, FPropertyTagData? tagData, ReadType type)
    {
        var tagType = propertyType switch
        {
            "ArrayProperty" => new ArrayProperty(Ar, tagData, type),
            "AssetObjectProperty" => new AssetObjectProperty(Ar, type),
            "AssetClassProperty" => new AssetObjectProperty(Ar, type),
            "BoolProperty" => new BoolProperty(Ar, tagData, type),
            "ByteProperty" => (tagData?.EnumName != null && !tagData.EnumName.Equals("None", StringComparison.OrdinalIgnoreCase)) || (type is ReadType.MAP && Ar.TestReadFName())
                ? (FPropertyTagType?) new EnumProperty(Ar, tagData, type)
                : new ByteProperty(Ar, type),
            "ClassProperty" => new ClassProperty(Ar, type),
            "DelegateProperty" => new DelegateProperty(Ar, type),
            "DoubleProperty" => new DoubleProperty(Ar, type),
            "EnumProperty" => new EnumProperty(Ar, tagData, type),
            "FieldPathProperty" => new FieldPathProperty(Ar, type),
            "FloatProperty" => new FloatProperty(Ar, type),
            "Int16Property" => new Int16Property(Ar, type),
            "Int64Property" => new Int64Property(Ar, type),
            "Int8Property" => new Int8Property(Ar, type),
            "IntProperty" => new IntProperty(Ar, type),
            "InterfaceProperty" => new InterfaceProperty(Ar, type),
            "LazyObjectProperty" => new LazyObjectProperty(Ar, type),
            "MapProperty" => new MapProperty(Ar, tagData, type),
            "MulticastDelegateProperty" => new MulticastDelegateProperty(Ar, type),
            "MulticastInlineDelegateProperty" => new MulticastInlineDelegateProperty(Ar, type),
            "MulticastSparseDelegateProperty" => new MulticastSparseDelegateProperty(Ar, type),
            "NameProperty" => new NameProperty(Ar, type),
            "ObjectProperty" => Ar is FLevelSaveRecordArchive ? new AssetObjectProperty(Ar, type) : new ObjectProperty(Ar, type), // ObjectProperty but serialized as string
            "SetProperty" => new SetProperty(Ar, tagData, type),
            "SoftClassProperty" => new SoftObjectProperty(Ar, type),
            "SoftObjectProperty" => new SoftObjectProperty(Ar, type),
            "StrProperty" => new StrProperty(Ar, type),
            "StructProperty" => new StructProperty(Ar, tagData, type),
            "TextProperty" => new TextProperty(Ar, type),
            "UInt16Property" => new UInt16Property(Ar, type),
            "UInt32Property" => new UInt32Property(Ar, type),
            "UInt64Property" => new UInt64Property(Ar, type),
            "WeakObjectProperty" => new WeakObjectProperty(Ar, type),
            "OptionalProperty" => new OptionalProperty(Ar, tagData, type),
            "VerseStringProperty" => new VerseStringProperty(Ar, type),
            "VerseFunctionProperty" => null,
            "VerseDynamicProperty" => new ObjectProperty(Ar, type), // idk, but for now read as ObjectProperty
            _ => null
        };
#if DEBUG
        if (tagType == null)
        {
            Log.Warning("Couldn't read property type {0} at {1}", propertyType, Ar.Position);
        }
#endif
        return tagType;
    }
}
