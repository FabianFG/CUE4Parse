using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Layout.Gameplay;

internal sealed class FProceduralPopulation(IEnumerable<FProceduralSpawnPoint> points, FProceduralPopulationSettings? settings = null)
{
    private readonly FProceduralPopulationSettings _settings = settings ?? new FProceduralPopulationSettings();
    private readonly Dictionary<FProceduralSpawnPoint, double> _weights = points
        .Where(point => double.IsFinite(point.Weight))
        .Distinct()
        .ToDictionary(point => point, point => Math.Max(0, point.Weight));

    public static List<FProceduralSpawnPoint> Select(IEnumerable<FProceduralSpawnPoint> points, int minimum, int maximum, float minDistance, ref FProceduralRandomStream random, CancellationToken ct)
        => new FProceduralPopulation(points).Select(minimum, maximum, minDistance, ref random, ct);
    public static List<FProceduralSpawnPoint> SelectCount(IEnumerable<FProceduralSpawnPoint> points, int count, float minDistance, ref FProceduralRandomStream random, CancellationToken ct)
        => new FProceduralPopulation(points).Select(count, count, minDistance, ref random, false, ct);
    private List<FProceduralSpawnPoint> Select(int minimum, int maximum, float minDistance, ref FProceduralRandomStream random, bool useCost, CancellationToken ct)
        => Select(minimum, maximum, minDistance, minDistance / _settings.TileSize, _settings.MapWidth - 1, ref random, useCost, ct);
    public List<FProceduralSpawnPoint> Select(int minimum, int maximum, float minDistance, ref FProceduralRandomStream random, CancellationToken ct)
        => Select(minimum, maximum, minDistance, ref random, true, ct);
    public List<FProceduralSpawnPoint> SelectCount(int count, float proximity, ref FProceduralRandomStream random, CancellationToken ct)
        => Select(count, count, 0, proximity, _settings.MapWidth, ref random, false, ct);
    private List<FProceduralSpawnPoint> Select(int minimum, int maximum, float minDistance, float proximity, int falloff, ref FProceduralRandomStream random, bool useCost, CancellationToken ct)
    {
        var remaining = _weights.Keys.Where(point => float.IsFinite(Cost(point)) && Cost(point) >= 0).ToList();
        var selected = new List<FProceduralSpawnPoint>();
        var rejected = new List<FProceduralSpawnPoint>();
        var cost = 0f;
        var minDistanceSquared = minDistance * minDistance;
        // Native visits every candidate, including those rejected after the budget is full
        while (remaining.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var priority = remaining.Where(point => _weights[point] > 0).Select(point => Math.Max(1, point.Priority)).DefaultIfEmpty(-1).Max();
            if (priority < 0)
                break;

            var totalWeight = remaining.Where(point => Math.Max(1, point.Priority) == priority).Sum(point => _weights[point]);
            var weight = random.NextFloat() * totalWeight;
            var index = remaining.FindLastIndex(point => Math.Max(1, point.Priority) == priority && _weights[point] > 0);
            for (var candidate = 0; candidate < remaining.Count; candidate++)
            {
                var point = remaining[candidate];
                if (Math.Max(1, point.Priority) != priority)
                    continue;

                if (weight < _weights[point])
                {
                    index = candidate;
                    break;
                }
                weight -= _weights[point];
            }

            var spawnPoint = remaining[index];
            remaining.RemoveAt(index);
            if (cost + Cost(spawnPoint) > maximum)
                continue;

            if (spawnPoint.WeightInfluenceable && selected.Any(other => other.WeightInfluencer && DistanceSquared(spawnPoint.Transform.Translation, other.Transform.Translation) < minDistanceSquared))
            {
                rejected.Add(spawnPoint);
                continue;
            }

            selected.Add(spawnPoint);
            cost += Cost(spawnPoint);
            if (proximity > 0 && spawnPoint.WeightInfluencer)
                AdjustWeights(spawnPoint.Transform.Translation, proximity, falloff, _settings.FactorToAdjustToWhenOutOfProximity);
        }

        rejected.AddRange(remaining);
        while (cost < minimum && rejected.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var point = rejected.MaxBy(candidate => (candidate.Priority, selected.Count == 0 ? 0 : selected.Min(other => DistanceSquared(candidate.Transform.Translation, other.Transform.Translation))))!;
            rejected.Remove(point);
            if (cost + Cost(point) > maximum)
                continue;

            selected.Add(point);
            cost += Cost(point);
        }

        return selected;

        float Cost(FProceduralSpawnPoint point) => useCost ? point.Cost : 1;
    }

    public void AdjustWeights(FVector position, float proximity, int falloff, float minimumFactor)
    {
        var tileArea = _settings.TileSize * _settings.TileSize;
        if (tileArea <= 0) tileArea = 1600 * 1600;
        if (falloff <= 0) falloff = 10;
        foreach (var point in _weights.Keys)
        {
            if (!point.WeightInfluenceable)
                continue;

            // FSpawnPopulationHandler::AdjustNearSpawnerWeights uses squared distance in tiles
            var distance = DistanceSquared(point.Transform.Translation, position) / tileArea;
            var factor = distance < proximity * proximity
                ? minimumFactor
                : Math.Max(Math.Max(minimumFactor, 0.01), Math.Min(distance / falloff, 1));

            _weights[point] *= factor;
        }
    }

    private float DistanceSquared(FVector first, FVector second)
    {
        var delta = first - second;
        if (MathF.Abs(delta.Z) > _settings.HeightAmplifierActivationHeight)
        {
            delta.Z *= _settings.HeightAmplifier;
        }

        return delta.SizeSquared();
    }
}
