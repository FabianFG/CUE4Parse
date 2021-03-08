using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports
{
    public interface IPropertyHolder
    {
        public List<FPropertyTag> Properties { get; }
    }

    [JsonConverter(typeof(UObjectConverter))]
    public class UObject : UExport, IPropertyHolder
    {
        public UObject? Outer;
        public UStruct? Class;
        public ResolvedObject? Template;
        public List<FPropertyTag> Properties { get; private set; }
        public bool ReadGuid { get; }
        public FGuid? ObjectGuid { get; private set; }
        public int /*EObjectFlags*/ Flags;

        // public FObjectExport Export;
        public override IPackage? Owner
        {
            get
            {
                var current = Outer;
                var next = current?.Outer;
                while (next != null)
                {
                    current = next;
                    next = current.Outer;
                }

                return current as IPackage;
            }
        }
        public override string ExportType => Class?.Name ?? GetType().Name;

        public UObject(FObjectExport exportObject, bool readGuid = true) : base(exportObject)
        {
            Properties = new List<FPropertyTag>();
            ReadGuid = readGuid;
        }

        public UObject() : base("")
        {
            Properties = new List<FPropertyTag>();
        }

        public UObject(List<FPropertyTag> properties) : base("")
        {
            Properties = properties;
        }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            if (Ar.HasUnversionedProperties)
            {
                var mappings = Ar.Owner.Mappings;
                if (mappings == null)
                    throw new ParserException("Found unversioned properties but package doesn't have any type mappings");
                Properties = DeserializePropertiesUnversioned(Ar, Class);
            }
            else
            {
                Properties = DeserializePropertiesTagged(Ar);
            }

            if ((Flags & 0x00000010) == 0 && Ar.ReadBoolean() && Ar.Position + 16 <= validPos)
            {
                ObjectGuid = Ar.Read<FGuid>();
            }
        }
        
        internal static List<FPropertyTag> DeserializePropertiesUnversioned(FAssetArchive Ar, UStruct struc)
        {
            var properties = new List<FPropertyTag>();
            var header = new FUnversionedHeader(Ar);
            if (!header.HasValues)
                return properties;
            var type = struc.Name;
            var propMappings = struc is UScriptClass ? Ar.Owner.Mappings?.Types[type] : new SerializedStruct(Ar.Owner.Mappings, struc);

            if (propMappings == null)
            {
                Log.Warning("Missing prop mappings for type {0} in package {1}", type, Ar.Owner.Name);
                return properties;
            }
            
            using var it = new FIterator(header);
            do
            {
                var (val, isNonZero) = it.Current;
                // The value has content and needs to be serialized normally
                if (isNonZero)
                {
                    if (propMappings.TryGetValue(val, out var propertyInfo))
                    {
                        var tag = new FPropertyTag(Ar, propertyInfo, ReadType.NORMAL);
                        if (tag.Tag != null)
                            properties.Add(tag);
                        else
                        {
                            Log.Warning(
                                "{0}: Failed to serialize property {1} of type. Can't proceed with serialization (Serialized {2} properties until now)",
                                type, propertyInfo.Name, propertyInfo.MappingType.Type, properties.Count);
                            return properties;
                        }
                    }
                    else
                    {
                        Log.Warning(
                            "{0}: Unknown property with value {1}. Can't proceed with serialization (Serialized {2} properties until now)",
                            type, val, properties.Count);
                        return properties;
                    }  
                }
                // The value is serialized as zero meaning we don't have to read any bytes here
                else
                {
                    if (propMappings.TryGetValue(val, out var propertyInfo))
                    {
                        properties.Add(new FPropertyTag(Ar, propertyInfo, ReadType.ZERO));
                    }
                    else
                    {
                        Log.Warning(
                            "{0}: Unknown property with value {1} but it's zero so we are good",
                            type, val);
                    }
                }
            } while (it.MoveNext());
            return properties;
        }
        
        internal static List<FPropertyTag> DeserializePropertiesTagged(FAssetArchive Ar)
        {
            var properties = new List<FPropertyTag>();
            while (true)
            {
                var tag = new FPropertyTag(Ar, true);
                if (tag.Name.IsNone)
                    break;
                properties.Add(tag);
            }

            return properties;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetOrDefault<T>(string name, T defaultValue = default, StringComparison comparisonType = StringComparison.Ordinal) =>
            PropertyUtil.GetOrDefault(this, name, defaultValue, comparisonType);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Lazy<T> GetOrDefaultLazy<T>(string name, T defaultValue = default, StringComparison comparisonType = StringComparison.Ordinal) =>
            PropertyUtil.GetOrDefaultLazy(this, name, defaultValue, comparisonType);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>(string name, StringComparison comparisonType = StringComparison.Ordinal) =>
            PropertyUtil.Get<T>(this, name, comparisonType);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Lazy<T> GetLazy<T>(string name, StringComparison comparisonType = StringComparison.Ordinal) =>
            PropertyUtil.GetLazy<T>(this, name, comparisonType);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetByIndex<T>(int index) => PropertyUtil.GetByIndex<T>(this, index);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue<T>(out T obj, params string[] names)
        {
            foreach (string name in names)
            {
                if (GetOrDefault<T>(name, comparisonType: StringComparison.OrdinalIgnoreCase) is T ret && !ret.Equals(default(T)))
                {
                    obj = ret;
                    return true;
                }
            }

            obj = default;
            return false;
        }
    }

    public static class PropertyUtil
    {
        // TODO Little Problem here: Can't use T? since this would need a constraint to struct or class, which again wouldn't work fine with primitives
        public static T GetOrDefault<T>(IPropertyHolder holder, string name, T defaultValue = default, StringComparison comparisonType = StringComparison.Ordinal)
        {
            foreach (var value in from it 
                in holder.Properties 
                where it.Name.Text.Equals(name, comparisonType)
                select it.Tag?.GetValue(typeof(T)))
            {
                if (value is T cast)
                    return cast;
            }

            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Lazy<T> GetOrDefaultLazy<T>(IPropertyHolder holder, string name, T defaultValue = default, 
            StringComparison comparisonType = StringComparison.Ordinal) =>
            new(() => GetOrDefault(holder, name, defaultValue, comparisonType));

        // Not optimal as well. Can't really compare against null or default. That's why this is a copy of GetOrDefault that throws instead
        public static T Get<T>(IPropertyHolder holder, string name, StringComparison comparisonType = StringComparison.Ordinal)
        {
            var tag = holder.Properties.FirstOrDefault(it => it.Name.Text.Equals(name, comparisonType))?.Tag;
            if (tag == null)
            {
                throw new NullReferenceException($"{holder.GetType().Name} does not have a property '{name}'");
            }
            var value = tag.GetValue(typeof(T));
            if (value is T cast)
            {
                return cast;
            }
            throw new NullReferenceException($"Couldn't get property '{name}' of type {typeof(T).Name} in {holder.GetType().Name}");
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Lazy<T> GetLazy<T>(IPropertyHolder holder, string name,
            StringComparison comparisonType = StringComparison.Ordinal) =>
            new(() => Get<T>(holder, name, comparisonType));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetByIndex<T>(IPropertyHolder holder, int index)
        {
            var tag = holder.Properties[index]?.Tag;
            if (tag == null)
            {
                throw new NullReferenceException($"{holder.GetType().Name} does not have a property at index '{index}'");
            }
            var value = tag.GetValue(typeof(T));
            if (value is T cast)
            {
                return cast;
            }
            throw new NullReferenceException($"Couldn't get property of type {typeof(T).Name} at index '{index}' in {holder.GetType().Name}");
        }
    }
    
    public class UObjectConverter : JsonConverter<UObject>
    {
        public override void WriteJson(JsonWriter writer, UObject value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            
            // export type
            writer.WritePropertyName("Type");
            writer.WriteValue(value.ExportType);
            
            if (!value.Name.Equals(value.ExportType))
            {
                writer.WritePropertyName("Name");
                writer.WriteValue(value.Name);
            }

            // export properties
            writer.WritePropertyName("Properties");
            writer.WriteStartObject();
            {
                foreach (var property in value.Properties)
                {
                    writer.WritePropertyName(property.Name.Text);
                    serializer.Serialize(writer, property.Tag);
                }
            }
            writer.WriteEndObject();
            
            writer.WriteEndObject();
        }

        public override UObject ReadJson(JsonReader reader, Type objectType, UObject existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}