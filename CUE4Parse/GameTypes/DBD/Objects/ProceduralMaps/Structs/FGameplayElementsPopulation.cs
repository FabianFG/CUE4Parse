using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

[StructFallback]
public readonly struct FGameplayElementsPopulation(FStructFallback fallback)
{
    public readonly EGameplayElementType Type = fallback.GetOrDefault<EGameplayElementType>(nameof(Type));
    public readonly ETileSpawnPointType TileSpawnPointType = fallback.GetOrDefault<ETileSpawnPointType>(nameof(TileSpawnPointType));
    public readonly FSoftObjectPath GameplayElementBlueprint = fallback.GetOrDefault<FSoftObjectPath>(nameof(GameplayElementBlueprint));
    public readonly int MinPopulation = fallback.GetOrDefault<int>(nameof(MinPopulation));
    public readonly int MaxPopulation = fallback.GetOrDefault<int>(nameof(MaxPopulation));
    public readonly int MinGuaranteePopulation = fallback.GetOrDefault<int>(nameof(MinGuaranteePopulation));
    public readonly int SpawnPass = fallback.GetOrDefault<int>(nameof(SpawnPass));
}
