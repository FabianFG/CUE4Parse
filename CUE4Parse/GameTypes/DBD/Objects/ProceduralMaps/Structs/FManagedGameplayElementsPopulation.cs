using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

[StructFallback]
public readonly struct FManagedGameplayElementsPopulation
{
    public readonly EGameplayElementType Type;
    public readonly FSoftObjectPath GameplayElementBlueprint;
    public readonly float MinDistance;
    public readonly int MinPopulation;
    public readonly int MaxPopulation;

    public FManagedGameplayElementsPopulation(FStructFallback fallback)
    {
        Type = fallback.GetOrDefault<EGameplayElementType>(nameof(Type));
        GameplayElementBlueprint = fallback.GetOrDefault<FSoftObjectPath>(nameof(GameplayElementBlueprint));
        MinDistance = fallback.GetOrDefault<float>(nameof(MinDistance));
        MinPopulation = fallback.GetOrDefault<int>(nameof(MinPopulation));
        MaxPopulation = fallback.GetOrDefault<int>(nameof(MaxPopulation));
    }

    public FManagedGameplayElementsPopulation(FManagedGameplayElementsPopulation population, FProceduralMap map)
    {
        Type = population.Type;
        GameplayElementBlueprint = population.GameplayElementBlueprint;
        (MinDistance, MinPopulation, MaxPopulation) = Type switch
        {
            EGameplayElementType.MeatLocker_Small => (map.HookMinDistance, map.HookMinCount, map.HookMaxCount),
            EGameplayElementType.Bookshelves => (map.BookShelvesMinDistance, map.BookShelvesMinCount, map.BookShelvesMaxCount),
            _ => (population.MinDistance, population.MinPopulation, population.MaxPopulation)
        };
        if (MinDistance < 0) MinDistance = population.MinDistance;
        if (MinPopulation < 0) MinPopulation = population.MinPopulation;
        if (MaxPopulation < 0) MaxPopulation = population.MaxPopulation;
    }
}
