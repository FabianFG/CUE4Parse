using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;

namespace CUE4Parse.GameTypes.GhostwireTokyo.Assets;

public static class GhostwireWorldResolver
{
    public static IReadOnlyList<UWorld> ResolveRootChildren(UWorld world, CancellationToken cancellationToken = default)
    {
        var provider = world.Owner?.Provider;
        if (provider?.Versions.Game != EGame.GAME_GhostwireTokyo)
            return [];

        var candidates = ResolveRootChildPaths(world, out var rootPath, out var worldName);
        if (candidates.Count == 0)
            return [];

        var result = new List<UWorld>(candidates.Count);
        var failedRoots = new List<(string Directory, string Prefix)>();

        foreach (var path in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (TryLoadWorld(provider, path, out var child))
                result.Add(child);
            else if (IsRoot(path))
                failedRoots.Add((GetDirectory(path), GetRootPrefix(path)));
        }

        if (failedRoots.Count == 0)
            return result;

        var selected = candidates.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var path in EnumerateCandidates(rootPath, worldName, provider.Files.Values.Select(x => x.Path)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (selected.Contains(path) || IsRoot(path) ||
                !failedRoots.Any(root => root.Directory.Equals(GetDirectory(path), StringComparison.OrdinalIgnoreCase) &&
                                         Path.GetFileNameWithoutExtension(path).StartsWith(root.Prefix, StringComparison.OrdinalIgnoreCase)))
                continue;
            if (TryLoadWorld(provider, path, out var child))
                result.Add(child);
        }

        return result;
    }

    public static IReadOnlyList<string> ResolveRootChildPaths(UWorld world, out string rootPath, out string worldName)
    {
        rootPath = "";
        worldName = world.Name;

        var provider = world.Owner?.Provider;
        if (provider?.Versions.Game != EGame.GAME_GhostwireTokyo)
            return [];

        var packageName = world.Owner?.Name;
        if (packageName is null || !packageName.StartsWith("/Game/", StringComparison.OrdinalIgnoreCase))
            return [];

        var suffix = packageName["/Game/".Length..] + ".umap";
        var foundRoot = provider.Files.Values.Select(x => Normalize(x.Path))
            .FirstOrDefault(x => x.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        if (foundRoot is null)
            return [];

        rootPath = foundRoot;

        // A top-level area entry map (one level below a "WorldComposition" directory) owns every map in
        // its area folder — the game links these without streaming-level entries or shared name prefixes
        // (CursedCamp_Main -> CursedCamp01..21 + CursedSchool01, GAct_Main -> GAct_Quest_*, Highway_Root ->
        // all highway blocks). Checked before the streaming-levels guard so the area is covered even when
        // the entry only streams part of it.
        if (IsAreaEntryPath(foundRoot))
            return SelectAreaScope(foundRoot, provider.Files.Values.Select(x => x.Path));

        if (world.StreamingLevels is { Length: > 0 })
            return [];

        // Top-level "world composition" root (e.g. 1stWorldComposition_PL): not a *_Root itself, but the
        // true game world — it owns every area's entry map one directory level below it
        // (DDG/DDG_Root, highway/Highway_Root, CursedCamp/CursedCamp_Main, ...).
        if (!world.Name.EndsWith("_Root", StringComparison.OrdinalIgnoreCase))
        {
            if (!world.Name.Contains("WorldComposition", StringComparison.OrdinalIgnoreCase))
                return [];
            return SelectTopLevelAreas(rootPath, provider.Files.Values.Select(x => x.Path));
        }

        // Deeper *_Root regions (SBY_Root, ...): prefix + direct-child-root + orphan selection.
        var availablePaths = provider.Files.Values.Select(x => x.Path).ToList();
        var candidateSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var prefixCandidates = SelectChildPaths(rootPath, world.Name, availablePaths);
        foreach (var path in prefixCandidates) candidateSet.Add(path);

        // Super-roots (region-composition roots such as DDG_Root) own sub-regions that each carry their
        // own name, so the prefix-scoped selection above finds nothing (no map is named "DDG_*"). Their
        // direct children are the sub-region roots, and each of those resolves its own content recursively.
        foreach (var path in SelectDirectChildRoots(rootPath, availablePaths)) candidateSet.Add(path);

        // Content roots (region / category roots) also own "fully orphaned" content: maps in their subtree
        // that no root's name prefix claims (special names like Shibuchika, cross-region names, typos such
        // as YNC_SE_*). Super-roots carry no prefix content, so they skip this sweep and let their region
        // children claim the orphans — otherwise the super-root would re-list the entire region.
        if (prefixCandidates.Count > 0)
        {
            foreach (var path in SelectOrphanContent(rootPath, world.Name, availablePaths)) candidateSet.Add(path);
        }

        return candidateSet.ToList();
    }

    private static IReadOnlyList<string> SelectChildPaths(string rootPath, string worldName, IEnumerable<string> availablePaths)
    {
        var candidates = EnumerateCandidates(rootPath, worldName, availablePaths).ToList();
        var roots = candidates.Where(IsRoot)
            .Select(x => (Directory: GetDirectory(x), Prefix: GetRootPrefix(x)))
            .ToArray();

        return candidates
            .Where(x => IsRoot(x) || !roots.Any(root =>
                root.Directory.Equals(GetDirectory(x), StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileNameWithoutExtension(x).StartsWith(root.Prefix, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<string> SelectDirectChildRoots(string rootPath, IEnumerable<string> availablePaths)
    {
        rootPath = Normalize(rootPath);
        var dir = GetDirectory(rootPath);
        var allRoots = availablePaths.Select(Normalize)
            .Where(x => x.StartsWith(dir, StringComparison.OrdinalIgnoreCase) &&
                        IsRoot(x) &&
                        !x.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Keep only the roots with no other root between them and this one — the direct sub-region roots.
        // Deeper roots (e.g. category roots under a region root) are reached by recursing into their parent.
        return allRoots
            .Where(r => allRoots.All(t => t.Equals(r, StringComparison.OrdinalIgnoreCase) || !IsAncestorRoot(t, r)))
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Entry maps of the top-level world: every <c>Area/Map.umap</c> directly under the root's directory
    /// (one subdirectory level), i.e. each gameplay area's root map — DDG_Root, Highway_Root,
    /// CursedCamp_Main, GAct_Main, LevelOfDetailRoot, ... Dev and Sound areas are excluded (editor/test
    /// and audio-only content). Deeper maps are reached through each entry map's own streaming levels.
    /// </summary>
    private static IReadOnlyList<string> SelectTopLevelAreas(string rootPath, IEnumerable<string> availablePaths)
    {
        rootPath = Normalize(rootPath);
        var dir = GetDirectory(rootPath);

        bool IsTopLevel(string x) => x[(dir.Length)..].Count(c => c == '/') == 1;
        string Area(string x) => x[dir.Length..].SubstringBeforeLast('/');

        return availablePaths.Select(Normalize)
            .Where(x => x.StartsWith(dir, StringComparison.OrdinalIgnoreCase) &&
                        x.EndsWith(".umap", StringComparison.OrdinalIgnoreCase) &&
                        !x.Equals(rootPath, StringComparison.OrdinalIgnoreCase) &&
                        IsTopLevel(x) &&
                        !AreaSkipList.Contains(Area(x)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>Top-level areas that are not part of the player-facing world (editor/test and audio-only).</summary>
    private static readonly string[] AreaSkipList = { "Dev", "Sound" };

    /// <summary>
    /// True for maps that sit exactly one directory level below a "WorldComposition" directory
    /// (e.g. .../WorldComposition/highway/Highway_Root.umap) — the entry maps of top-level game areas.
    /// </summary>
    private static bool IsAreaEntryPath(string path)
    {
        path = Normalize(path);
        const string marker = "WorldComposition/";
        var idx = path.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return false;
        var rel = path[(idx + marker.Length)..];
        return rel.EndsWith(".umap", StringComparison.OrdinalIgnoreCase) && rel.Count(c => c == '/') == 1;
    }

    /// <summary>
    /// Every map in the area folder of a top-level entry map (any depth), excluding the entry itself.
    /// </summary>
    private static IReadOnlyList<string> SelectAreaScope(string entryPath, IEnumerable<string> availablePaths)
    {
        var dir = GetDirectory(Normalize(entryPath));
        return availablePaths.Select(Normalize)
            .Where(x => x.StartsWith(dir, StringComparison.OrdinalIgnoreCase) &&
                        x.EndsWith(".umap", StringComparison.OrdinalIgnoreCase) &&
                        !x.Equals(entryPath, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsAncestorRoot(string potentialAncestor, string descendant)
    {
        var ancestorDir = GetDirectory(potentialAncestor);
        return descendant.StartsWith(ancestorDir, StringComparison.OrdinalIgnoreCase) &&
               GetDirectory(descendant) != ancestorDir;
    }

    /// <summary>
    /// Content maps in the root's subtree that no root's name prefix claims — special named content,
    /// cross-region references, and typos (e.g. YNC_SE_*). These are still part of the world and must export.
    /// </summary>
    private static IReadOnlyList<string> SelectOrphanContent(string rootPath, string worldName, IEnumerable<string> availablePaths)
    {
        rootPath = Normalize(rootPath);
        var dir = GetDirectory(rootPath);
        var prefix = worldName.EndsWith("Root", StringComparison.OrdinalIgnoreCase)
            ? worldName[..^"Root".Length]
            : worldName;

        return availablePaths.Select(Normalize)
            .Where(x => x.StartsWith(dir, StringComparison.OrdinalIgnoreCase) &&
                        x.EndsWith(".umap", StringComparison.OrdinalIgnoreCase) &&
                        !x.Equals(rootPath, StringComparison.OrdinalIgnoreCase) &&
                        !IsRoot(x) &&
                        !Path.GetFileNameWithoutExtension(x).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> EnumerateCandidates(string rootPath, string worldName, IEnumerable<string> availablePaths)
    {
        rootPath = Normalize(rootPath);
        var directory = GetDirectory(rootPath);
        var prefix = worldName[..^"Root".Length];

        return availablePaths.Select(Normalize)
            .Where(x => x.StartsWith(directory, StringComparison.OrdinalIgnoreCase))
            .Where(x => x.EndsWith(".umap", StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
            .Where(x => Path.GetFileNameWithoutExtension(x).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public static bool TryLoadWorld(IFileProvider provider, string path, out UWorld world)
    {
        world = null!;
        if (!provider.TryLoadPackage(path, out var package))
            return false;
        world = package.GetExports().OfType<UWorld>().FirstOrDefault()!;
        return world is not null;
    }

    public static bool IsRoot(string path) =>
        Path.GetFileNameWithoutExtension(path).EndsWith("_Root", StringComparison.OrdinalIgnoreCase);

    private static string GetRootPrefix(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return name[..^"Root".Length];
    }

    private static string GetDirectory(string path)
    {
        path = Normalize(path);
        var separator = path.LastIndexOf('/');
        return separator >= 0 ? path[..(separator + 1)] : string.Empty;
    }

    private static string Normalize(string path) => path.Replace('\\', '/');
}
