using CUE4Parse.UE4.IO.Objects;

namespace CUE4Parse.GameTypes.LordOfMysteries;

public sealed class LoMDirectoryIndex
{
    private readonly Dictionary<ulong, string> _packagePaths;

    private LoMDirectoryIndex(Dictionary<ulong, string> packagePaths)
    {
        _packagePaths = packagePaths;
    }

    public bool TryGetPackagePath(FIoChunkId chunkId, out string path)
    {
        if (!_packagePaths.TryGetValue(chunkId.ChunkId, out var packagePath))
        {
            path = string.Empty;
            return false;
        }

        var extension = GetExtension(chunkId.ChunkType);
        if (extension.Length == 0)
        {
            path = string.Empty;
            return false;
        }

        path = chunkId.ChunkType == (byte) EIoChunkType5.ExportBundleData ? packagePath : Path.ChangeExtension(packagePath, extension);
        return true;
    }

    public static LoMDirectoryIndex Read(DirectoryInfo workingDirectory)
    {
        var packagePaths = new Dictionary<ulong, string>();
        string? fileListPath = null;
        for (var current = workingDirectory; current != null; current = current.Parent)
        {
            fileListPath = Path.Combine(current.FullName, "Manifest_UFSFiles_Win64.txt");
            if (File.Exists(fileListPath))
                break;

            fileListPath = null;
        }

        if (fileListPath == null)
            return new LoMDirectoryIndex(packagePaths);

        foreach (var line in File.ReadLines(fileListPath))
        {
            if (TryReadPackagePath(line, out var path, out var packageName))
            {
                packagePaths.TryAdd(FPackageId.FromName(packageName).id, path);
            }
        }

        return new LoMDirectoryIndex(packagePaths);
    }

    private static bool TryReadPackagePath(string line, out string path, out string packageName)
    {
        path = string.Empty;
        packageName = string.Empty;

        var tabIndex = line.IndexOf('\t');
        if (tabIndex >= 0)
        {
            line = line[..tabIndex];
        }

        line = line.Trim().Trim('"').Replace('\\', '/');
        var extension = Path.GetExtension(line);
        if (!extension.Equals(".uasset", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".umap", StringComparison.OrdinalIgnoreCase))
            return false;

        var contentIndex = line.IndexOf("/Content/", StringComparison.OrdinalIgnoreCase);
        if (contentIndex < 0)
            return false;

        path = line;
        var relativePath = line[(contentIndex + "/Content/".Length)..^extension.Length];
        if (line.StartsWith("C7/Content/", StringComparison.OrdinalIgnoreCase))
        {
            packageName = "/Game/" + relativePath;
            return true;
        }

        if (line.StartsWith("Engine/Content/", StringComparison.OrdinalIgnoreCase))
        {
            packageName = "/Engine/" + relativePath;
            return true;
        }

        var pluginPrefix = "/Plugins/";
        var pluginIndex = line.IndexOf(pluginPrefix, StringComparison.OrdinalIgnoreCase);
        if (pluginIndex >= 0)
        {
            var pluginNameStart = pluginIndex + pluginPrefix.Length;
            var pluginNameEnd = line.IndexOf('/', pluginNameStart);
            if (pluginNameEnd > pluginNameStart)
            {
                packageName = "/" + line[pluginNameStart..pluginNameEnd] + "/" + relativePath;
                return true;
            }
        }

        return false;
    }

    private static string GetExtension(byte chunkType) => (EIoChunkType5) chunkType switch
    {
        EIoChunkType5.ExportBundleData => "uasset",
        EIoChunkType5.BulkData => "ubulk",
        EIoChunkType5.OptionalBulkData => "uptnl",
        EIoChunkType5.MemoryMappedBulkData => "m.ubulk",
        EIoChunkType5.ShaderCodeLibrary => "ushaderbytecode",
        EIoChunkType5.ShaderCode => "dxbc",
        _ => string.Empty // ummm, rip?
    };
}
