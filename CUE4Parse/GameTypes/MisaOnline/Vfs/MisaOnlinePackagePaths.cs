using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.GameTypes.MisaOnline.Vfs;

internal static class MisaOnlinePackagePaths
{
    public static Dictionary<FPackageId, string> Read(FArchive registryArchive, Func<string, string> fixPath)
    {
        var registry = new FPartialAssetRegistryState(registryArchive);
        var paths = new Dictionary<FPackageId, string>();
        foreach (var asset in registry.PreallocatedAssetDataBuffers)
        {
            var packageName = asset.PackageName.Text;
            if (string.IsNullOrEmpty(packageName))
                continue;

            var path = fixPath(packageName);
            if (asset.AssetClass.Text == "World" && path.EndsWith(".uasset", StringComparison.OrdinalIgnoreCase))
                path = path[..^7] + ".umap";

            var id = FPackageId.FromName(packageName);
            if (!paths.TryAdd(id, path) && path.EndsWith(".umap", StringComparison.OrdinalIgnoreCase))
                paths[id] = path;
        }

        return paths;
    }
}
