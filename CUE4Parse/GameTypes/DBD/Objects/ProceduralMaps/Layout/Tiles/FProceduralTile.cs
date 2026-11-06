using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Tiles;

public sealed record FProceduralTile(string AssetPath, UBlueprintGeneratedClass Blueprint)
{
    public ETileType Type { get; init; }
    public EDensity Density { get; init; }
    public EPathType PathType { get; init; }
    public int Number { get; init; }
    public int Width { get; init; } = 1;
    public int Height { get; init; } = 1;
    public int MaximumCount { get; init; }
    public int MinimumSpacing { get; init; }
    public double Weight { get; init; } = 1.0;
    public bool bReplacement { get; init; }
    public bool IsEscapeTile { get; init; }

    internal bool MatchesDensity(EDensity density) => density is EDensity.Unspecified || Density is EDensity.Unspecified || Density == density;

    public static FProceduralTile FromBlueprint(string assetPath, UBlueprintGeneratedClass blueprint)
    {
        var defaults = blueprint.ClassDefaultObject?.Load() ?? throw new InvalidDataException($"Tile Blueprint '{assetPath}' has no class default object.");
        var dimensions = defaults.GetOrDefault("Dimension", new FVector2D(1, 1));

        var isEscapeTile = false;
        var visited = new HashSet<UBlueprintGeneratedClass>();
        for (var current = blueprint; current != null && visited.Add(current); current = current.SuperStruct?.Load() as UBlueprintGeneratedClass)
            isEscapeTile |= current.SuperStruct?.Name == "EscapeTile";

        return new FProceduralTile(assetPath, blueprint)
        {
            Type = defaults.GetOrDefault(nameof(Type), ETileType.None),
            Density = defaults.GetOrDefault<EDensity>(nameof(Density)),
            PathType = defaults.GetOrDefault("Path", EPathType.None),
            Number = defaults.GetOrDefault(nameof(Number), 0),
            Width = Math.Max(1, (int) dimensions.X),
            Height = Math.Max(1, (int) dimensions.Y),
            MaximumCount = defaults.GetOrDefault(nameof(MaximumCount), 0),
            MinimumSpacing = defaults.GetOrDefault(nameof(MinimumSpacing), 0),
            Weight = defaults.GetOrDefault(nameof(Weight), 1.0),
            bReplacement = defaults.GetOrDefault("Replacement", false),
            IsEscapeTile = isEscapeTile
        };
    }
}
