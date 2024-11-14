using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Objects
{
    [JsonConverter(typeof(FStructFallbackConverter))]
    [SkipObjectRegistration]
    public class FStructFallback : IUStruct, IPropertyHolder
    {
        public List<FPropertyTag> Properties { get; }

        public FStructFallback()
        {
            Properties = new List<FPropertyTag>();
        }

        public FStructFallback(FAssetArchive Ar, string? structType) : this(Ar, structType != null ? new UScriptClass(structType) : null) { }

        public FStructFallback(FAssetArchive Ar, UStruct? structType = null)
        {
            if (Ar.HasUnversionedProperties)
            {
                if (structType == null) throw new ArgumentException("For unversioned struct fallback the struct type cannot be null", nameof(structType));
                UObject.DeserializePropertiesUnversioned(Properties = new List<FPropertyTag>(), Ar, structType);
            }
            else
            {
                UObject.DeserializePropertiesTagged(Properties = new List<FPropertyTag>(), Ar, true);
            }
        }

        public T GetOrDefault<T>(string name, T defaultValue = default!, StringComparison comparisonType = StringComparison.Ordinal) =>
            PropertyUtil.GetOrDefault<T>(this, name, defaultValue, comparisonType);

        public Lazy<T> GetOrDefaultLazy<T>(string name, T defaultValue = default,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            throw new NotImplementedException();
        }

        public T Get<T>(string name, StringComparison comparisonType = StringComparison.Ordinal) =>
            PropertyUtil.Get<T>(this, name, comparisonType);

        public Lazy<T> GetLazy<T>(string name, StringComparison comparisonType = StringComparison.Ordinal)
        {
            throw new NotImplementedException();
        }

        public T GetByIndex<T>(int index)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue<T>(out T obj, params string[] names)
        {
            foreach (string name in names)
            {
                if (this.TryGet<T>(name, out obj, comparisonType: StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            obj = default!;
            return false;
        }
        public bool TryGetAllValues<T>(out T[] obj, string name)
        {
            var maxIndex = -1;
            var collected = new List<FPropertyTag>();
            foreach (var prop in Properties)
            {
                if (prop.Name.Text != name) continue;
                collected.Add(prop);
                maxIndex = Math.Max(maxIndex, prop.ArrayIndex);
            }

            obj = new T[maxIndex + 1];
            foreach (var prop in collected) {
                obj[prop.ArrayIndex] = (T)prop.Tag?.GetValue(typeof(T))!;
            }

            return obj.Length > 0;
        }
    }
}
