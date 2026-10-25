using CUE4Parse.GameTypes.DBD.Assets.Exports.Components;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Blueprints;

public static class FActorSpawnerVisualization
{
    public static bool IsEventAsset(string? path) => path?.Contains("/SpecialEvents/", StringComparison.OrdinalIgnoreCase) == true;

    public static UBlueprintGeneratedClass? GetBlueprint(UObject template, bool? activated = null)
    {
        if (template.Game is not GAME_DeadByDaylight)
            return null;

        switch (template)
        {
            case UActorVariationSpawner variationSpawner when variationSpawner.Visualization.TryLoad<UBlueprintGeneratedClass>(out var variationClass):
                return IsEventAsset(variationClass.GetPathName()) ? null : variationClass;
            case UActorSpawner { Type: EGameplayElementType.Generic } spawner:
                var active = activated ?? spawner.ActivatedByDefault;
                if (!active || spawner.ComponentTags.Any(tag => tag.Text.StartsWith("Quadrant_", StringComparison.Ordinal)))
                    break;

                // Visualization can reference a removed asset, fallback to spawn content
                foreach (var path in spawner.ActivatedSceneElement.Where(element => float.IsFinite(element.Weight) && element.Weight > 0).Select(element => element.SceneElement).Prepend(spawner.Visualization))
                {
                    if (path.TryLoad<UBlueprintGeneratedClass>(out var childClass))
                        return IsEventAsset(childClass.GetPathName()) ? null : childClass;
                }
                break;
        }

        return null;
    }
}
