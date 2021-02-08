using System;
using System.Reflection;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    public enum ReadType : byte
    {
        ZERO,
        NORMAL,
        MAP,
        ARRAY
    }

    public abstract class FPropertyTagType<T> : FPropertyTagType
    {
        public T Value { get; protected set; }

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
                case FPropertyTagType<UScriptStruct> structProp when type.IsInstanceOfType(structProp.Value.StructType):
                    return structProp.Value.StructType;
                case FPropertyTagType<UScriptStruct> structProp when structProp.Value.StructType is FStructFallback fallback && type.GetCustomAttribute<StructFallback>() != null:
                    return fallback.MapToClass(type);
                case FPropertyTagType<UScriptArray> arrayProp when type.IsArray:
                    var array = arrayProp.Value.Properties;
                    var contentType = type.GetElementType()!;
                    var result = Array.CreateInstance(contentType, array.Count);
                    for (var i = 0; i < array.Count; i++)
                    {
                        result.SetValue(array[i].GetValue(contentType), i);
                    }
                    return result;
                case FPropertyTagType<FPackageIndex> objProp when typeof(UExport).IsAssignableFrom(type):
                    if (objProp.Value.TryLoad(out var objExport) && type.IsInstanceOfType(objExport))
                        return objExport;
                    return null;
                case FPropertyTagType<FSoftObjectPath> softObjProp when typeof(UExport).IsAssignableFrom(type):
                    if (softObjProp.Value.TryLoad(out var softExport) && type.IsInstanceOfType(softExport))
                        return softExport;
                    return null;
                case EnumProperty enumProp when type.IsEnum:
                    var storedEnum = enumProp.Value.Text;
                    if (type.Name != storedEnum.SubstringBefore("::"))
                        return null;
                
                    var search = storedEnum.SubstringAfter("::");
                    var values = type.GetEnumNames()!;
                    var idx = Array.FindIndex(values, it => it == search);
                    return idx == -1 ? null : type.GetEnumValues().GetValue(idx);
                //TODO There are also Enums stored as ByteProperty but UModel uses them nowhere besides in UE2
                //TODO Maybe Maps?
                default:
                    return null;
            }
        }

        public abstract override string ToString();

        internal static FPropertyTagType? ReadPropertyTagType(FAssetArchive Ar, string propertyType, FPropertyTagData? tagData, ReadType type)
        {
            var tagType = propertyType switch
            {
                "ByteProperty" => tagData?.EnumName != null
                    ? (FPropertyTagType?) new EnumProperty(Ar, tagData, type)
                    : new ByteProperty(Ar, type),
                "BoolProperty" => new BoolProperty(Ar, tagData, type),
                "IntProperty" => new IntProperty(Ar, type),
                "FloatProperty" => new FloatProperty(Ar, type),
                "ObjectProperty" => new ObjectProperty(Ar, type),
                "NameProperty" => new NameProperty(Ar, type),
                "DelegateProperty" => new DelegateProperty(Ar, type),
                "DoubleProperty" => new DoubleProperty(Ar, type),
                "StrProperty" => new StrProperty(Ar, type),
                "TextProperty" => new TextProperty(Ar, type),
                "InterfaceProperty" => new InterfaceProperty(Ar, type),
                "SoftObjectProperty" => new SoftObjectProperty(Ar, type),
                "AssetObjectProperty" => new SoftObjectProperty(Ar, type),
                "UInt64Property" => new UInt64Property(Ar, type),
                "UInt32Property" => new UInt32Property(Ar, type),
                "UInt16Property" => new UInt16Property(Ar, type),
                "Int64Property" => new Int64Property(Ar, type),
                "Int16Property" => new Int16Property(Ar, type),
                "Int8Property" => new Int8Property(Ar, type),
                "EnumProperty" => new EnumProperty(Ar, tagData, type),
                "ArrayProperty" => new ArrayProperty(Ar, tagData, type),
                "SetProperty" => new SetProperty(Ar, tagData, type),
                "StructProperty" => new StructProperty(Ar, tagData, type),
                "MapProperty" => new MapProperty(Ar, tagData, type),
                _ => null
            };
#if DEBUG
            if (tagType == null)
            {
                Console.WriteLine($"Couldn't read property type {propertyType} at {Ar.Position}");  
            }      
#endif
            return tagType;
        }
    }
    
    public class FPropertyTagTypeConverter : JsonConverter<FPropertyTagType>
    {
        public override void WriteJson(JsonWriter writer, FPropertyTagType value, JsonSerializer serializer)
        {
            // serializer.Serialize(writer, value);
            switch (value) // remove switch once all types are added
            {
                case ObjectProperty o:
                    serializer.Serialize(writer, o);
                    break;
                case TextProperty t:
                    serializer.Serialize(writer, t);
                    break;
                case StructProperty s:
                    serializer.Serialize(writer, s);
                    break;
                case SoftObjectProperty so:
                    serializer.Serialize(writer, so);
                    break;
                case EnumProperty e:
                    serializer.Serialize(writer, e);
                    break;
                case ArrayProperty a:
                    serializer.Serialize(writer, a);
                    break;
                case BoolProperty b:
                    serializer.Serialize(writer, b);
                    break;
                case FloatProperty f:
                    serializer.Serialize(writer, f);
                    break;
                case IntProperty i:
                    serializer.Serialize(writer, i);
                    break;
                case StrProperty st:
                    serializer.Serialize(writer, st);
                    break;
                case NameProperty n:
                    serializer.Serialize(writer, n);
                    break;
                case ByteProperty by:
                    serializer.Serialize(writer, by);
                    break;
                case MapProperty m:
                    serializer.Serialize(writer, m);
                    break;
                default:
                    writer.WriteNull();
                    break;
            }
        }

        public override FPropertyTagType ReadJson(JsonReader reader, Type objectType, FPropertyTagType existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
