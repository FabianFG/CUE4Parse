using System;
using System.Collections.Generic;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets
{

    public interface IPackage
    {
        public string Name { get; set; }
        public IFileProvider? Provider { get; }
        public TypeMappings? Mappings { get; }

        public FPackageFileSummary Summary { get; }
        public FNameEntrySerialized[] NameMap { get; }
        public Lazy<UObject>[] ExportsLazy { get; }

        public abstract bool IsFullyLoaded { get; }

        public bool HasFlags(EPackageFlags flags);
        /*public T? GetExportOfTypeOrNull<T>() where T : UObject;
        public T GetExportOfType<T>() where T : UObject;*/
        public UObject? GetExportOrNull(string name, StringComparison comparisonType = StringComparison.Ordinal);
        public T? GetExportOrNull<T>(string name, StringComparison comparisonType = StringComparison.Ordinal) where T : UObject;
        public UObject GetExport(string name, StringComparison comparisonType = StringComparison.Ordinal);
        public T GetExport<T>(string name, StringComparison comparisonType = StringComparison.Ordinal) where T : UObject;
        public Lazy<UObject>? FindObject(FPackageIndex? index);
        public ResolvedObject? ResolvePackageIndex(FPackageIndex? index);
        public UObject? GetExport(int index);
        public IEnumerable<UObject> GetExports();
    }
}