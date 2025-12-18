using System;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.AoC.Objects;

public class FakePackage : IPackage
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

    public FakePackage(string name, TypeMappings? mappings)
    {
        Name = name;
        Mappings = mappings;
    }

    public bool HasFlags(EPackageFlags flags)
    {
        return EPackageFlags.PKG_FilterEditorOnly.HasFlag(flags);
    }

    public int GetExportIndex(string name, StringComparison comparisonType = StringComparison.Ordinal)
    {
        throw new NotImplementedException();
    }

    public ResolvedObject? ResolvePackageIndex(FPackageIndex? index)
    {
        return null;
    }
}
