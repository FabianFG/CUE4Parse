using CUE4Parse.GameTypes.DBD.Assets.Exports;
using CUE4Parse.GameTypes.DBD.Assets.Exports.Components;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Blueprints;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Tiles;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Quadrants;

internal static class FProceduralQuadrantPlacementBuilder
{
    private static readonly string[] _cornerTags = ["Quadrant_BottomRight", "Quadrant_BottomLeft", "Quadrant_TopLeft", "Quadrant_TopRight"];

    public static IReadOnlyList<FProceduralActorPlacement> Load(FProceduralTileCell[] cells, int width, int height,
        FVector2D tileSize, IReadOnlyList<FProceduralTilePlacement> tiles, UProceduralGenerationData data, int seed, float weightDecay, CancellationToken ct)
    {
        var definitions = FProceduralQuadrantLayout.Build(cells, width, height, data.QuadrantSpawnTypeProperties, ct);
        var tilesByCell = new FProceduralTilePlacement?[cells.Length];
        foreach (var tile in tiles)
        {
            for (var y = tile.Y; y < tile.Y + tile.Height; y++)
            {
                for (var x = tile.X; x < tile.X + tile.Width; x++)
                {
                    tilesByCell[y * width + x] = tile;
                }
            }
        }

        var spawnersByBlueprint = new Dictionary<UBlueprintGeneratedClass, IReadOnlyList<(UActorSpawner Spawner, FTransform Transform)>>();
        var categories = data.QuadrantSpawnCategories.ToDictionary(category => category.QuadrantSpawnTag.Text, StringComparer.OrdinalIgnoreCase);
        var pools = new Dictionary<FWeightedItem[], FWeightedItem[]>();
        var environmentOffset = FProceduralTileLayout.EnvironmentTransform(tileSize, width, height).Translation;
        var placements = new List<FProceduralActorPlacement>();
        var random = new FProceduralRandomStream(seed, EProceduralRandomStream.QuadrantSelection);
        foreach (var definition in definitions)
        {
            ct.ThrowIfCancellationRequested();
            if (tilesByCell[definition.Y * width + definition.X] is not { } tile)
                continue;

            if (!spawnersByBlueprint.TryGetValue(tile.Tile.Blueprint, out var spawners))
            {
                spawners = FProceduralComponentReader.ReadSpawners(tile.Tile.Blueprint);
                spawnersByBlueprint.Add(tile.Tile.Blueprint, spawners);
            }

            var localCorner = (definition.Corner - cells[definition.Y * width + definition.X].Rotation / 90 + 4) % 4;
            foreach (var (spawner, localTransform) in spawners)
            {
                if (spawner.Type is not EGameplayElementType.QuadrantSpawn || spawner.Weight <= 0 || !spawner.ComponentTags.Any(tag => tag.Text.Equals(_cornerTags[localCorner], StringComparison.OrdinalIgnoreCase)))
                    continue;

                var categoryTag = spawner.ComponentTags.FirstOrDefault(tag => categories.ContainsKey(tag.Text));
                if (!categories.TryGetValue(categoryTag.Text, out var category))
                    continue;

                var poolType = definition.Type is EQuadrantSpawnType.Rect_X or EQuadrantSpawnType.Rect_Y ? EQuadrantSpawnType.Rect : definition.Type;
                var pool = category.Items.FirstOrDefault(item => item.Type == poolType).Elements;
                if (pool == null)
                    continue;

                if (!pools.TryGetValue(pool, out var candidates))
                    pools.Add(pool, candidates = [.. pool]);

                var selected = Select(candidates, random.NextUInt32(), weightDecay);
                if (selected is not { } item)
                    continue;

                // Native advances this stream ten times when preparing the selected quadrant's content
                for (var step = 0; step < 10; step++)
                {
                    random.NextUInt32();
                }

                if (!item.Element.TryLoad<UBlueprintGeneratedClass>(out var blueprint))
                    continue;

                var transform = localTransform * tile.Transform(tileSize, width, height);
                transform = OrientRoom(transform, definition, cells, width, height, tileSize, environmentOffset);
                placements.Add(new FProceduralActorPlacement($"Quadrant_{definition.X}_{definition.Y}_{definition.Corner}", blueprint, transform));
                break;
            }
        }

        return placements;
    }

    internal static FWeightedItem? Select(FWeightedItem[] items, uint randomValue, float weightDecay = 1)
    {
        var totalWeight = items.Sum(item => Math.Max(0, item.Weight));
        if (totalWeight <= 0)
            return null;

        // FRandomStream::GetFraction() https://dev.epicgames.com/documentation/unreal-engine/API/Runtime/Core/FRandomStream
        var fraction = BitConverter.UInt32BitsToSingle(0x3F800000u | randomValue >> 9) - 1.0f;
        var remainingWeight = fraction * totalWeight;
        for (var index = 0; index < items.Length; index++)
        {
            var item = items[index];
            if (item.Weight <= 0)
                continue;
            if (remainingWeight <= item.Weight)
            {
                items[index] = new FWeightedItem(item.Element, item.Weight * weightDecay);
                return item;
            }

            remainingWeight -= item.Weight;
        }

        return null;
    }

    private static FTransform OrientRoom(FTransform transform, FProceduralQuadrant definition, FProceduralTileCell[] cells, int width, int height, FVector2D tileSize, FVector environmentOffset)
    {
        var yaw = NormalizeYaw((int) MathF.Round(transform.Rotation.Rotator().Yaw) + definition.RotationOffset);
        if (definition.Type is EQuadrantSpawnType.Rect_X && yaw % 180 != 0 || definition.Type is EQuadrantSpawnType.Rect_Y && yaw % 180 == 0)
            yaw = (yaw + 90) % 360;

        var originalPosition = transform.Translation - environmentOffset;
        var cellX = (int) (originalPosition.X / tileSize.X);
        var cellY = (int) (originalPosition.Y / tileSize.Y);
        if (cellX >= 0 && cellX < width && cellY >= 0 && cellY < height)
        {
            var forcedYaw = cells[cellY * width + cellX].QuadrantRotation switch
            {
                1 => 0,
                2 => 180,
                3 => 270,
                4 => 90,
                _ => -1
            };

            var compatibleAxis = definition.Type switch
            {
                EQuadrantSpawnType.Rect_X => forcedYaw % 180 == 0,
                EQuadrantSpawnType.Rect_Y => forcedYaw % 180 == 90,
                _ => true
            };

            if (forcedYaw >= 0 && compatibleAxis && forcedYaw != yaw)
            {
                if (definition.Type is EQuadrantSpawnType.Rect_X)
                {
                    transform.Translation.X += forcedYaw == 0 ? -tileSize.X : tileSize.X;
                }
                else if (definition.Type is EQuadrantSpawnType.Rect_Y)
                {
                    transform.Translation.Y += forcedYaw == 90 ? -tileSize.Y : tileSize.Y;
                }

                yaw = forcedYaw;
            }
        }

        transform.Rotation = new FQuat(new FRotator(0, yaw, 0));
        return transform;
    }

    private static int NormalizeYaw(int yaw) => (yaw % 360 + 360) % 360;
}
