using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Objects.Unversioned;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using Newtonsoft.Json;
using Serilog;

namespace CUE4Parse.UE4.Assets.Exports
{
    public interface IPropertyHolder
    {
        public List<FPropertyTag> Properties { get; }
    }

    [JsonConverter(typeof(UObjectConverter))]
    public class UObject : IPropertyHolder
    {
        public string Name { get; set; }
        public UObject? Outer;
        public UStruct? Class;
        public ResolvedObject? Super;
        public ResolvedObject? Template;
        public List<FPropertyTag> Properties { get; private set; }
        public FGuid? ObjectGuid { get; private set; }
        public EObjectFlags Flags;

        // public FObjectExport Export;
        public IPackage? Owner
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
        public virtual string ExportType => Class?.Name ?? GetType().Name;

        public UObject()
        {
            Properties = new List<FPropertyTag>();
        }

        public UObject(List<FPropertyTag> properties)
        {
            Properties = properties;
        }

        public virtual void Deserialize(FAssetArchive Ar, long validPos)
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

            if (!Flags.HasFlag(EObjectFlags.RF_ClassDefaultObject) && Ar.ReadBoolean() && Ar.Position + 16 <= validPos)
            {
                ObjectGuid = Ar.Read<FGuid>();
            }
        }

        /**
         * Returns the fully qualified pathname for this object as well as the name of the class, in the format:
         * 'ClassName Outermost.[Outer:]Name'.
         *
         * @param   stopOuter   if specified, indicates that the output string should be relative to this object.  if StopOuter
         *                      does not exist in this object's Outer chain, the result would be the same as passing NULL.
         */
        public string GetFullName(UObject? stopOuter = null, bool includeClassPackage = false)
        {
            var result = new StringBuilder(128);
            GetFullName(stopOuter, result, includeClassPackage);
            return result.ToString();
        }

        public void GetFullName(UObject? stopOuter, StringBuilder resultString, bool includeClassPackage = false)
        {
            resultString.Append(includeClassPackage ? Class?.GetPathName() : ExportType);
            resultString.Append(' ');
            GetPathName(stopOuter, resultString);
        }

        /**
         * Returns the fully qualified pathname for this object, in the format:
         * 'Outermost[.Outer].Name'
         *
         * @param   stopOuter   if specified, indicates that the output string should be relative to this object.  if stopOuter
         *                      does not exist in this object's outer chain, the result would be the same as passing null.
         */
        public string GetPathName(UObject? stopOuter = null)
        {
            var result = new StringBuilder();
            GetPathName(stopOuter, result);
            return result.ToString();
        }

        /**
         * Versions of getPathName() that eliminates unnecessary copies and allocations.
         */
        public void GetPathName(UObject? stopOuter, StringBuilder resultString)
        {
            if (this != stopOuter)
            {
                var objOuter = Outer;
                if (objOuter != null && objOuter != stopOuter)
                {
                    objOuter.GetPathName(stopOuter, resultString);
                    // SUBOBJECT_DELIMITER_CHAR is used to indicate that this object's outer is not a UPackage
                    resultString.Append(objOuter.Outer is IPackage ? ':' : '.');
                }

                resultString.Append(Name);
            }
            else
            {
                resultString.Append("None");
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

        /** 
	     * Do any object-specific cleanup required immediately after loading an object, 
	     * and immediately after any undo/redo.
	     */
        public virtual void PostLoad()
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

        protected internal virtual void WriteJson(JsonWriter writer, JsonSerializer serializer)
        {
            var package = Owner;

            // export type
            writer.WritePropertyName("Type");
            writer.WriteValue(ExportType);

            // object name
            writer.WritePropertyName("Name"); // ctrl click depends on the name, we always need it
            writer.WriteValue(Name);

            // outer
            if (Outer != null && Outer != package)
            {
                writer.WritePropertyName("Outer");
                writer.WriteValue(Outer.Name); // TODO serialize the path too
            }

            // super
            if (Super != null)
            {
                writer.WritePropertyName("Super");
                writer.WriteValue(Super.Name.Text);
            }

            // template
            if (Template != null)
            {
                writer.WritePropertyName("Template");
                writer.WriteValue(Template.Name.Text);
            }

            // export properties
            if (Properties.Count > 0)
            {
                writer.WritePropertyName("Properties");
                writer.WriteStartObject();
                foreach (var property in Properties)
                {
                    writer.WritePropertyName(property.Name.Text);
                    serializer.Serialize(writer, property.Tag);
                }

                writer.WriteEndObject();
            }
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

        public override string ToString() => GetFullName();
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
            value.WriteJson(writer, serializer);
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

        public FLifetimeProperty(int repIndex, ELifetimeCondition condition, ELifetimeRepNotifyCondition repNotifyCondition = ELifetimeRepNotifyCondition.REPNOTIFY_OnChanged)
        {
            RepIndex = (ushort) repIndex;
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