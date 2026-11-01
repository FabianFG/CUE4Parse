using CUE4Parse.GameTypes.DBD.Assets.Exports;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Tiles;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;
using CUE4Parse.FileProvider;
using CUE4Parse.FileProvider.Objects;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Assets;

internal sealed class FProceduralMapAssets(IFileProvider provider, string tilesDirectory)
{
    private const string ThemePropertiesTableName = "LevelGen_ThemePropertiesDB";
    private const string LightingTableName = "LevelGen_LightDataDB";
    private const string ProceduralMapsTableName = "ProceduralMaps";

    private readonly ILookup<string, GameFile> _tables = FindTables(provider);
    private readonly UGameTypeModifierDefaults? _modifiers = provider.SafeLoadPackageObject<UGameTypeModifierDefaults>("/Game/Data/GameTypeModifierDefaults.GameTypeModifierDefaults");

    public int LoadEscapeDoorCount() => Math.Max(0, _modifiers?.GetIntModifier(EGameTypeModifierName.EscapeDoorCount) ?? 0);
    public int LoadHatchCount() => Math.Max(0, _modifiers?.GetIntModifier(EGameTypeModifierName.EscapeHatchCount, 1) ?? 1);
    public int LoadChestCountModifier() => _modifiers?.GetIntModifier(EGameTypeModifierName.InGameChestCountModifier) ?? 0;
    public int LoadGeneratorCount()
    {
        var count = _modifiers?.GetIntModifier(EGameTypeModifierName.GeneratorCount, -1) ?? -1;
        // Negative modifier fallbacks to the match rules, 7 is default
        return count >= 0 ? count : 7;
    }

    public List<FProceduralTile> LoadTiles(string tilePath, CancellationToken ct)
    {
        var directories = GetContentDirectories(provider, tilesDirectory).ToArray();
        var directorySuffix = '/' + tilesDirectory.Trim('/') + '/';
        var tiles = new List<FProceduralTile>();
        foreach (var file in provider.Files.Values.Where(file => file.IsUePackage && file.Path.Contains(directorySuffix, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            var directory = directories.FirstOrDefault(directory => file.Path.StartsWith(directory.FilePath, StringComparison.OrdinalIgnoreCase));
            if (directory.FilePath == null)
                continue;

            var packagePath = directory.PackagePath + file.PathWithoutExtension[directory.FilePath.Length..];
            if (!packagePath.Contains(tilePath, StringComparison.OrdinalIgnoreCase))
                continue;

            var package = provider.LoadPackage(file);
            if (package.GetExportOrNull<UBlueprintGeneratedClass>(file.NameWithoutExtension + "_C") is not { } blueprint)
                continue;

            var tile = FProceduralTile.FromBlueprint(file.Path, blueprint);
            if (tile.Type is not ETileType.None)
                tiles.Add(tile);
        }

        if (tiles.Count == 0)
            throw new InvalidDataException($"No tile Blueprints found for theme path '{tilePath}' in '{tilesDirectory}'.");

        return tiles;
    }

    public FThemeProperties ResolveTheme(string packagePath, CancellationToken ct)
    {
        foreach (var (key, row) in ReadRows(ThemePropertiesTableName, ct))
        {
            var theme = new FThemeProperties(row, key);
            if (string.IsNullOrEmpty(theme.MapPath) || !packagePath.Contains(theme.MapPath, StringComparison.OrdinalIgnoreCase))
                continue;

            return theme;
        }

        throw new InvalidDataException($"No procedural theme is defined for tile map '{packagePath}'.");
    }

    public UBlueprintGeneratedClass? LoadLighting(FName themeName, CancellationToken ct)
    {
        foreach (var (key, row) in ReadRows(LightingTableName, ct))
        {
            var lighting = new FSceneLightingDescription(row);
            var name = lighting.ThemeName.IsNone ? key : lighting.ThemeName;
            if (!string.Equals(name.Text, themeName.Text, StringComparison.OrdinalIgnoreCase))
                continue;

            if (lighting.LightingDetailsHighUltra.LightBlueprint.TryLoad<UBlueprintGeneratedClass>(out var blueprint))
                return blueprint;
        }

        return null;
    }

    public FProceduralMap? LoadMap(string mapName, CancellationToken ct)
    {
        foreach (var (key, row) in ReadRows(ProceduralMapsTableName, ct))
        {
            if (key.Text.Equals(mapName, StringComparison.OrdinalIgnoreCase))
                return new FProceduralMap(row);
        }

        return null;
    }

    private IEnumerable<KeyValuePair<FName, FStructFallback>> ReadRows(string name, CancellationToken ct)
    {
        foreach (var file in _tables[name])
        {
            ct.ThrowIfCancellationRequested();
            if (provider.SafeLoadPackageObject<UDataTable>(file.Path, name)?.RowMap is not { } rows)
                continue;

            foreach (var row in rows)
            {
                ct.ThrowIfCancellationRequested();
                yield return row;
            }
        }
    }

    private static IEnumerable<(string PackagePath, string FilePath)> GetContentDirectories(IFileProvider provider, string directory)
    {
        directory = directory.Trim('/');
        var path = $"/Game/{directory}/";
        yield return (path, provider.FixPath(path));

        var pluginsPath = provider.FixPath("/Game/Plugins/");
        foreach (var plugin in provider.VirtualPaths.OrderBy(plugin => plugin.Key, StringComparer.Ordinal))
        {
            if (!plugin.Value.StartsWith(pluginsPath, StringComparison.OrdinalIgnoreCase))
                continue;

            path = $"/{plugin.Key}/{directory}/";
            yield return (path, provider.FixPath(path));
        }
    }

    private static ILookup<string, GameFile> FindTables(IFileProvider provider)
    {
        var directories = GetContentDirectories(provider, "Data").Select(directory => directory.FilePath).ToArray();
        return provider.Files.Values
            .Where(file => file.IsUePackage && file.NameWithoutExtension is ThemePropertiesTableName or LightingTableName or ProceduralMapsTableName)
            .Where(file => directories.Any(directory => file.Path.StartsWith(directory, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(file => file.Path, StringComparer.Ordinal)
            .ToLookup(file => file.NameWithoutExtension, StringComparer.OrdinalIgnoreCase);
    }
}
