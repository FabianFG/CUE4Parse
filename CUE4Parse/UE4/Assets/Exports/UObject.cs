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

        public UObject(FObjectExport exportObject) : base(exportObject)
        {
            Properties = new List<FPropertyTag>();
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
                if (Class == null)
                    throw new ParserException(Ar, "Found unversioned properties but object does not have a class");
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

        /** 
         * Walks up the list of outers until it finds the highest one.
         *
         * @return outermost non NULL Outer.
         */
        public AbstractUePackage? GetOutermost()
        {
            var top = this;
            for (;;)
            {
                var currentOuter = top.Outer;
                if (currentOuter == null)
                {
                    if (top is not AbstractUePackage)
                    {
                        Log.Warning("GetOutermost expects an IPackage as outermost object but '{Top}' isn't one", top);
                    }

                    return top as AbstractUePackage;
                }
                top = currentOuter;
            }
        }

        public override void PostLoad()
        {
            
        }

        internal static List<FPropertyTag> DeserializePropertiesUnversioned(FAssetArchive Ar, UStruct struc)
        {
            var properties = new List<FPropertyTag>();
            var header = new FUnversionedHeader(Ar);
            if (!header.HasValues)
                return properties;
            var type = struc.Name;
            
            Struct? propMappings = null;
            if (struc is UScriptClass)
                Ar.Owner.Mappings?.Types.TryGetValue(type, out propMappings);
            else
                propMappings = new SerializedStruct(Ar.Owner.Mappings, struc);

            if (propMappings == null)
            {
                throw new ParserException(Ar, "Missing prop mappings for type " + type);
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
                            throw new ParserException(Ar, $"{type}: Failed to serialize property {propertyInfo.MappingType.Type} {propertyInfo.Name}. Can't proceed with serialization (Serialized {properties.Count} properties until now)");
                        }
                    }
                    else
                    {
                        throw new ParserException(Ar, $"{type}: Unknown property with value {val}. Can't proceed with serialization (Serialized {properties.Count} properties until now)");
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
        
        // Just ignore it for the parser
        /*-----------------------------------------------------------------------------
	        Replication.
        -----------------------------------------------------------------------------*/

        /** Returns properties that are replicated for the lifetime of the actor channel */
        public virtual void GetLifetimeReplicatedProps(List<FLifetimeProperty> outLifetimeProps)
        {
            
        }

        /** Called right before receiving a bunch */
        public virtual void PreNetReceive()
        {
            
        }

        /** Called right after receiving a bunch */
        public virtual void PostNetReceive()
        {
            
        }

        /** Called right before being marked for destruction due to network replication */
        public virtual void PreDestroyFromReplication()
        {
            
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
    
    // ~Fabian: Please just ignore that, needed it in a different project

    /** FLifetimeProperty
     *	This class is used to track a property that is marked to be replicated for the lifetime of the actor channel.
     *  This doesn't mean the property will necessarily always be replicated, it just means:
     *	"check this property for replication for the life of the actor, and I don't want to think about it anymore"
     *  A secondary condition can also be used to skip replication based on the condition results
     */
    public class FLifetimeProperty
    {
        public ushort RepIndex;
        public ELifetimeCondition Condition;
        public ELifetimeRepNotifyCondition RepNotifyCondition;

        public FLifetimeProperty()
        {
            RepIndex = 0;
            Condition = ELifetimeCondition.COND_None;
            RepNotifyCondition = ELifetimeRepNotifyCondition.REPNOTIFY_OnChanged;
        }

        public FLifetimeProperty(int repIndex)
        {
            RepIndex = (ushort) repIndex;
            Condition = ELifetimeCondition.COND_None;
            RepNotifyCondition = ELifetimeRepNotifyCondition.REPNOTIFY_OnChanged;
        }

        public FLifetimeProperty(ushort repIndex, ELifetimeCondition condition, ELifetimeRepNotifyCondition repNotifyCondition = ELifetimeRepNotifyCondition.REPNOTIFY_OnChanged)
        {
            RepIndex = repIndex;
            Condition = condition;
            RepNotifyCondition = repNotifyCondition;
        }

        protected bool Equals(FLifetimeProperty other)
        {
            return RepIndex == other.RepIndex && Condition == other.Condition && RepNotifyCondition == other.RepNotifyCondition;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FLifetimeProperty) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RepIndex, (int) Condition, (int) RepNotifyCondition);
        }

        public static bool operator ==(FLifetimeProperty a, FLifetimeProperty b) => a.RepIndex == b.RepIndex && a.Condition == b.Condition && a.RepNotifyCondition == b.RepNotifyCondition;
        public static bool operator !=(FLifetimeProperty a, FLifetimeProperty b) => !(a == b);
    }
}