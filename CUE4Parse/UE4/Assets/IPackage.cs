using System;
using System.Collections.Generic;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets
{
    public enum PackageFlags : uint
    {
        UnversionedProperties = 0x00002000    // UE4.25+
    }
    
    public interface IPackage
    {
        public string Name { get; }
        public IFileProvider? Provider { get; }
        public TypeMappings? Mappings { get; }
        
        public FPackageFileSummary Summary { get; }
        public FNameEntrySerialized[] NameMap { get; }
        public Lazy<UObject>[] ExportsLazy { get; }

        public bool HasFlags(PackageFlags flags);
        /*public T? GetExportOfTypeOrNull<T>() where T : UExport;
        public T GetExportOfType<T>() where T : UExport;*/
        public UExport? GetExportOrNull(string name, StringComparison comparisonType = StringComparison.Ordinal);
        public T? GetExportOrNull<T>(string name, StringComparison comparisonType = StringComparison.Ordinal) where T : UExport;
        public UExport GetExport(string name, StringComparison comparisonType = StringComparison.Ordinal);
        public T GetExport<T>(string name, StringComparison comparisonType = StringComparison.Ordinal) where T : UExport;
        public Lazy<UObject>? FindObject(FPackageIndex? index);
        public ResolvedObject? ResolvePackageIndex(FPackageIndex? index);
        public UExport? GetExport(int index);
        public IEnumerable<UExport> GetExports();
    }
}