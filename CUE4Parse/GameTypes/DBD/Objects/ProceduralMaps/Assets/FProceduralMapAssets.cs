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

internal sealed class FProceduralMapAssets(IFileProvider provider)
{
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

    public FThemeProperties ResolveTheme(string packagePath, CancellationToken ct)
    {
        var directories = packagePath.Split('/');
        foreach (var (key, row) in ReadRows("LevelGen_ThemePropertiesDB", ct))
        {
            var theme = new FThemeProperties(row, key);
            if (string.IsNullOrEmpty(theme.MapPath) || !directories.Contains(theme.MapPath, StringComparer.OrdinalIgnoreCase))
                continue;

            return theme;
        }

        throw new InvalidDataException($"No procedural theme is defined for tile map '{packagePath}'.");
    }

    public List<FProceduralTile> LoadTiles(string tilePath, CancellationToken ct)
    {
        var prefix = provider.FixPath($"/Game/Blueprints/Tiles/{tilePath}/");
        var tiles = new List<FProceduralTile>();
        foreach (var file in provider.Files.Values.Where(f => f.IsUePackage && f.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).OrderBy(f => f.Path, StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            var package = provider.LoadPackage(file);
            if (package.GetExportOrNull<UBlueprintGeneratedClass>(file.NameWithoutExtension + "_C") is not { } blueprint)
                continue;

            var tile = FProceduralTile.FromBlueprint(file.Path, blueprint);
            if (tile.Type is not ETileType.None)
                tiles.Add(tile);
        }

        if (tiles.Count == 0)
            throw new InvalidDataException($"No tile Blueprints found in '{prefix}'.");

        return tiles;
    }

    public UBlueprintGeneratedClass? LoadLighting(FName themeName, CancellationToken ct)
    {
        foreach (var (key, row) in ReadRows("LevelGen_LightDataDB", ct))
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
        foreach (var (key, row) in ReadRows("ProceduralMaps", ct))
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

    // Theme and lighting rows can live in either the base database or a DLC database
    private static ILookup<string, GameFile> FindTables(IFileProvider provider)
    {
        var dataPath = provider.FixPath("/Game/Data/");
        var dlcPath = provider.FixPath("/Game/Data/Dlc/");
        return provider.Files.Values
            .Where(file => file.IsUePackage && file.NameWithoutExtension is "LevelGen_ThemePropertiesDB" or "LevelGen_LightDataDB" or "ProceduralMaps")
            .Where(file => file.Path.Equals(dataPath + file.Name, StringComparison.OrdinalIgnoreCase) || file.Path.StartsWith(dlcPath, StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => file.Path, StringComparer.Ordinal)
            .ToLookup(file => file.NameWithoutExtension, StringComparer.OrdinalIgnoreCase);
    }
}
