using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets
{
    public abstract class AbstractUePackage : UObject, IPackage
    {
        public IFileProvider? Provider { get; }
        public TypeMappings? Mappings { get; }
        public abstract FPackageFileSummary Summary { get; }
        public abstract FNameEntrySerialized[] NameMap { get; }
        public abstract Lazy<UObject>[] ExportsLazy { get; }
        
        public AbstractUePackage(string name, IFileProvider? provider, TypeMappings? mappings)
        {
            Name = name;
            Provider = provider;
            Mappings = mappings;
        }

        protected static UObject ConstructObject(UStruct? struc)
        {
            UObject? obj = null;
            var current = struc;
            while (current != null) // Traverse up until a known one is found
            {
                if (current is UScriptClass scriptClass)
                {
                    obj = scriptClass.ConstructObject();
                    break;
                }

                current = current.SuperStruct.Load<UStruct>();
            }

            obj ??= new UObject();
            obj.Class = struc;
            return obj;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasFlags(PackageFlags flags) => Summary.PackageFlags.HasFlag(flags);

        /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? GetExportOfTypeOrNull<T>() where T : UObject
        {
            var export = ExportMap.FirstOrDefault(it => typeof(T).IsAssignableFrom(it.ExportType));
            try
            {
                return export?.ExportObject.Value as T;
            }
            catch (Exception e)
            {
                Log.Debug(e, "Failed to get export object");
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetExportOfType<T>() where T : UObject =>
            GetExportOfTypeOrNull<T>() ??
            throw new NullReferenceException($"Package '{Name}' does not have an export of type {typeof(T).Name}");*/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract UObject? GetExportOrNull(string name, StringComparison comparisonType = StringComparison.Ordinal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? GetExportOrNull<T>(string name, StringComparison comparisonType = StringComparison.Ordinal)
            where T : UObject => GetExportOrNull(name, comparisonType) as T;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UObject GetExport(string name, StringComparison comparisonType = StringComparison.Ordinal) =>
            GetExportOrNull(name, comparisonType) ??
            throw new NullReferenceException(
                $"Package '{Name}' does not have an export with the name '{name}'");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetExport<T>(string name, StringComparison comparisonType = StringComparison.Ordinal) where T : UObject =>
            GetExportOrNull<T>(name, comparisonType) ??
            throw new NullReferenceException(
                $"Package '{Name}' does not have an export with the name '{name} and type {typeof(T).Name}'");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UObject? GetExport(int index) => index < ExportsLazy.Length ? ExportsLazy[index].Value : null;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<UObject> GetExports() => ExportsLazy.Select(x => x.Value);

        public Lazy<UObject>? FindObject(FPackageIndex? index)
        {
            if (index == null || index.IsNull) return null;
            if (index.IsImport) return ResolvePackageIndex(index)?.Object;
            return ExportsLazy[index.Index - 1];
        }

        public abstract ResolvedObject? ResolvePackageIndex(FPackageIndex? index);

        public override string ToString() => Name;
    }

    [JsonConverter(typeof(ResolvedObjectConverter))]
    public abstract class ResolvedObject
    {
        public readonly IPackage Package;

        public ResolvedObject(IPackage package, int index)
        {
            Package = package;
            Index = index;
        }

        public int Index { get; }
        public abstract FName Name { get; }
        public virtual ResolvedObject? Outer => null;
        public virtual ResolvedObject? Class => null;
        public virtual ResolvedObject? Super => null;
        public virtual Lazy<UObject>? Object => null;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UObject Load(IFileProvider provider) => Object.Value;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryLoad(IFileProvider provider, out UObject export)
        {
            try
            {
                export = Object.Value;
                return true;
            }
            catch
            {
                export = default;
                return false;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<UObject> LoadAsync(IFileProvider provider) => await Task.FromResult(Object.Value);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<UObject?> TryLoadAsync(IFileProvider provider)
        {
            try
            {
                return await Task.FromResult(Object.Value);
            }
            catch
            {
                return await Task.FromResult<UObject?>(null);
            }
        }
        
        public override string ToString()
        {
            return $"{Name.Text} ({Class.Name})";
        }
    }
    
    public class ResolvedObjectConverter : JsonConverter<ResolvedObject>
    {
        public override void WriteJson(JsonWriter writer, ResolvedObject value, JsonSerializer serializer)
        {
            var outerChain = new List<string>();
            var current = value.Outer;
            while (current != null)
            {
                outerChain.Add(current.Name.Text);
                current = current.Outer;
            }
            
            writer.WriteStartObject();
            
            writer.WritePropertyName("ObjectName"); // 1:2:3 if we are talking about an export in the current asset
            writer.WriteValue($"{(outerChain.Count > 1 ? $"{outerChain[0]}:" : "")}{value.Name.Text}:{value.Class?.Name}");

            writer.WritePropertyName("ObjectPath"); // package path . index
            if (outerChain.Count <= 0) writer.WriteValue(value.Index);
            else writer.WriteValue($"{outerChain[outerChain.Count - 1]}.{value.Index}");

            writer.WriteEndObject();
        }

        public override ResolvedObject ReadJson(JsonReader reader, Type objectType, ResolvedObject existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class ResolvedLoadedObject : ResolvedObject
    {
        private readonly UObject _object;

        public ResolvedLoadedObject(int index, UObject obj) : base(obj.Owner, index)
        {
            _object = obj;
        }

        public override FName Name => new(_object.Name);
        public override ResolvedObject? Outer
        {
            get
            {
                var obj = _object.Outer;
                return obj != null ? new ResolvedLoadedObject(Index, obj) : null;
            }
        }
        public override ResolvedObject? Class
        {
            get
            {
                var obj = _object.Class;
                return obj != null ? new ResolvedLoadedObject(Index, obj) : null;
            }
        }
        public override ResolvedObject? Super => null; //new ResolvedLoadedObject(_object.Super);
        public override Lazy<UObject> Object => new(() => _object);
    }
}