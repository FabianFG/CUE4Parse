using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports
{
    public interface IPropertyHolder
    {
        public List<FPropertyTag> Properties { get; }
    }
    public class UObject : UExport, IPropertyHolder
    {
        public List<FPropertyTag> Properties { get; }
        public bool ReadGuid { get; }
        public FGuid? ObjectGuid { get; private set; }

        public UObject(FObjectExport exportObject, bool readGuid = true) : base(exportObject)
        {
            Properties = new List<FPropertyTag>();
            ReadGuid = readGuid;
        }

        public UObject() : this(new List<FPropertyTag>(), null, "")
        {
            ExportType = GetType().Name;
            Name = ExportType;
        }

        public UObject(List<FPropertyTag> properties, FGuid? objectGuid, string exportType) : base(exportType)
        {
            Properties = properties;
            ObjectGuid = objectGuid;
        }

        public override void Deserialize(FAssetArchive Ar)
        {
            while (true)
            {
                var tag = new FPropertyTag(Ar, true);
                if (tag.Name.IsNone)
                    break;
                Properties.Add(tag);
            }

            if (ReadGuid && Ar.ReadBoolean() && Ar.Position + 16 <= Ar.Length)
            {
                ObjectGuid = Ar.Read<FGuid>();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetOrDefault<T>(string name, StringComparison comparisonType = StringComparison.Ordinal) =>
            PropertyUtil.GetOrDefault<T>(this, name, comparisonType);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>(string name, StringComparison comparisonType = StringComparison.Ordinal) =>
            PropertyUtil.Get<T>(this, name, comparisonType);
    }

    public static class PropertyUtil
    {
        // TODO Little Problem here: Can't use T? since this would need a constraint to struct or class, which again wouldn't work fine with primitives
        public static T GetOrDefault<T>(IPropertyHolder holder, string name, StringComparison comparisonType = StringComparison.Ordinal)
        {
            var value = holder.Properties.FirstOrDefault(it => it.Name.Text.Equals(name, comparisonType))?.Tag?.GetValue(typeof(T));
            if (value is T cast)
            {
                return cast;
            }
            return default;
        }

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
    }
}