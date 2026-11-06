using CUE4Parse.GameTypes.DBD.Assets.Exports.Components;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Blueprints;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.UE4.Assets.Exports.Engine;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Tiles;

internal static class FProceduralVariationLayout
{
    public static void Build(IReadOnlyList<FProceduralTilePlacement> placements, FProceduralTileCell[] cells, int width, CancellationToken ct)
    {
        foreach (var placement in placements)
        {
            ct.ThrowIfCancellationRequested();
            var tag = (ETileVariation) cells[placement.Y * width + placement.X].Tag;
            if (tag is ETileVariation.None)
                continue;

            var variations = new Dictionary<string, UBlueprintGeneratedClass>(StringComparer.Ordinal);
            foreach (var (component, _) in FProceduralComponentReader.ReadComponents<UActorVariationSpawner>(placement.Tile.Blueprint))
            {
                foreach (var variation in component.Variations)
                {
                    if (variation.Type != tag || !variation.Element.TryLoad<UBlueprintGeneratedClass>(out var blueprint))
                        continue;

                    var name = component.Name;
                    if (name.EndsWith(USimpleConstructionScript.ComponentTemplateNameSuffix, StringComparison.Ordinal))
                        name = name[..^USimpleConstructionScript.ComponentTemplateNameSuffix.Length];
                    variations[name] = blueprint;
                    break;
                }
            }

            if (variations.Count > 0)
                placement.ComponentVariations = variations;
        }
    }
}
