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

        (bool result, path) = (EIoChunkType5) chunkId.ChunkType switch
        {
            EIoChunkType5.ExportBundleData or EIoChunkType5.ShaderCodeLibrary => (true, packagePath),
            EIoChunkType5.BulkData => (true, Path.ChangeExtension(packagePath, "ubulk")),
            EIoChunkType5.OptionalBulkData => (true, Path.ChangeExtension(packagePath, "uptnl")),
            _ => (false, string.Empty)
        };

        return result;
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

    private static readonly HashSet<string> _extensions = new(StringComparer.OrdinalIgnoreCase) { ".uasset", ".umap", ".ushaderbytecode" };
    private static readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> _extensionsLookup = _extensions.GetAlternateLookup<ReadOnlySpan<char>>();

    private static bool TryReadPackagePath(string line, out string path, out string packageName)
    {
        path = string.Empty;
        packageName = string.Empty;

        var tabIndex = line.IndexOf('\t');
        var lineSpan = line.AsSpan();
        if (tabIndex >= 0)
        {
            lineSpan = lineSpan[..tabIndex].Trim();
        }

        var extension = Path.GetExtension(lineSpan);
        if (!_extensionsLookup.Contains(extension))
            return false;

        var contentIndex = lineSpan.IndexOf("/Content/", StringComparison.OrdinalIgnoreCase);
        if (contentIndex < 0)
            return false;

        path = lineSpan.ToString();
        var relativePath = lineSpan[(contentIndex + "/Content/".Length)..^extension.Length];

        const string shaderArchives = "C7/Content/ShaderArchive-";
        if (path.StartsWith(shaderArchives, StringComparison.OrdinalIgnoreCase))
        {
            packageName = path[shaderArchives.Length..^extension.Length];
            return true;
        }

        if (path.StartsWith("C7/Content/", StringComparison.OrdinalIgnoreCase))
        {
            packageName = string.Concat("/Game/", relativePath);
            return true;
        }

        if (path.StartsWith("Engine/Content/", StringComparison.OrdinalIgnoreCase))
        {
            packageName = string.Concat("/Engine/", relativePath);
            return true;
        }

        // Plugins
        if (path.StartsWith("Engine/Plugins/Interchange/Assets/Content", StringComparison.OrdinalIgnoreCase))
        {
            packageName = string.Concat("/InterchangeAssets/", relativePath);
            return true;
        }
        else if (path.StartsWith("Engine/Plugins/Interchange/Runtime/Content", StringComparison.OrdinalIgnoreCase))
        {
            packageName = string.Concat("/Interchange/", relativePath);
            return true;
        }

        // Generic plugins
        var pluginRootIndex = path.IndexOf("/Plugins/", StringComparison.OrdinalIgnoreCase);
        if (pluginRootIndex >= 0)
        {
            var beforeContent = lineSpan[..contentIndex];
            var lastSlashBeforeContent = beforeContent.LastIndexOf('/');
            if (lastSlashBeforeContent >= 0 && lastSlashBeforeContent < beforeContent.Length - 1)
            {
                var pluginName = beforeContent[(lastSlashBeforeContent + 1)..];

                packageName = string.Concat("/", pluginName, "/", relativePath);
                return true;
            }
        }

        return false;
    }
}
