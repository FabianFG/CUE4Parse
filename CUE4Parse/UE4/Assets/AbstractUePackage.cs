using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;

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
                if (current is UScriptClass h)
                {
                    obj = h.ConstructObject();
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
        public T? GetExportOfTypeOrNull<T>() where T : UExport
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
        public T GetExportOfType<T>() where T : UExport =>
            GetExportOfTypeOrNull<T>() ??
            throw new NullReferenceException($"Package '{Name}' does not have an export of type {typeof(T).Name}");*/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public abstract UExport? GetExportOrNull(string name, StringComparison comparisonType = StringComparison.Ordinal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? GetExportOrNull<T>(string name, StringComparison comparisonType = StringComparison.Ordinal)
            where T : UExport => GetExportOrNull(name, comparisonType) as T;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UExport GetExport(string name, StringComparison comparisonType = StringComparison.Ordinal) =>
            GetExportOrNull(name, comparisonType) ??
            throw new NullReferenceException(
                $"Package '{Name}' does not have an export with the name '{name}'");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetExport<T>(string name, StringComparison comparisonType = StringComparison.Ordinal) where T : UExport =>
            GetExportOrNull<T>(name, comparisonType) ??
            throw new NullReferenceException(
                $"Package '{Name}' does not have an export with the name '{name} and type {typeof(T).Name}'");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<UExport> GetExports() => ExportsLazy.Select(x => x.Value);

        public Lazy<UObject>? FindObject(FPackageIndex? index)
        {
            if (index == null || index.IsNull) return null;
            if (index.IsImport) return ResolvePackageIndex(index)?.Object;
            return ExportsLazy[index.Index - 1];
        }

        public abstract ResolvedObject? ResolvePackageIndex(FPackageIndex? index);

        public override string ToString() => Name;
    }

    public abstract class ResolvedObject
    {
        public IPackage Package;

        public ResolvedObject(IPackage package)
        {
            Package = package;
        }

        public abstract FName Name { get; }
        public virtual ResolvedObject? Outer => null;
        public virtual ResolvedObject? Super => null;
        public virtual Lazy<UObject>? Object => null;
    }
}