using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets;

public interface IPackage
{
    public string Name { get; set; }
    public IFileProvider? Provider { get; }
    public TypeMappings? Mappings { get; }

    public FPackageFileSummary Summary { get; }
    public FNameEntrySerialized[] NameMap { get; }
    public int ImportMapLength { get; }
    public int ExportMapLength { get; }

    public Lazy<UObject>[] ExportsLazy { get; }
    public bool IsFullyLoaded { get; }
    public bool CanDeserialize { get; }

    public bool HasFlags(EPackageFlags flags);

    public int GetExportIndex(string name, StringComparison comparisonType = StringComparison.Ordinal);
    public ResolvedObject? ResolvePackageIndex(FPackageIndex? index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UObject? GetExportOrNull(string name, StringComparison comparisonType = StringComparison.Ordinal)
        => GetExport(GetExportIndex(name, comparisonType));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? GetExportOrNull<T>(string name, StringComparison comparisonType = StringComparison.Ordinal) where T : UObject
        => GetExportOrNull(name, comparisonType) as T;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UObject GetExport(string name, StringComparison comparisonType = StringComparison.Ordinal)
        => GetExportOrNull(name, comparisonType) ??
           throw new NullReferenceException($"Package '{Name}' does not have an export with the name '{name}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetExport<T>(string name, StringComparison comparisonType = StringComparison.Ordinal) where T : UObject
        => GetExportOrNull<T>(name, comparisonType) ??
           throw new NullReferenceException($"Package '{Name}' does not have an export with the name '{name} and type {typeof(T).Name}'");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Lazy<UObject>? FindObject(FPackageIndex? index) => ResolvePackageIndex(index)?.Object;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Lazy<T>? FindObject<T>(FPackageIndex? index) where T : UObject => FindObject(index) as Lazy<T>;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UObject? GetExport(int index) => index >= 0 && index < ExportsLazy.Length ? ExportsLazy[index].Value : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<UObject> GetExports(int start, int count) => ExportsLazy.Skip(start).Take(count).Select(export => export.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<UObject> GetExports() => ExportsLazy.Select(export => export.Value);
}
