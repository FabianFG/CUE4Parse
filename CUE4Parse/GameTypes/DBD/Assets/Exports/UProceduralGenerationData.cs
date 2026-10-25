using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Assets.Exports;

public class UProceduralGenerationData : UDataAsset
{
    public FQuadrantSpawnCategories[] QuadrantSpawnCategories = [];
    public FQuadrantSpawnTypeProperties[] QuadrantSpawnTypeProperties = [];
    public FGameplayElementsPopulation[] ElementsPopulation = [];
    public FManagedGameplayElementsPopulation[] ManagedElementsPopulation = [];
    public FPackageIndex? SurvivorItemPopulationProbability;
    public float ProceduralTileWidth = 1600;
    public float ProceduralTileHeight = 1600;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Game is not GAME_DeadByDaylight)
            return;

        QuadrantSpawnCategories = GetOrDefault(nameof(QuadrantSpawnCategories), QuadrantSpawnCategories);
        QuadrantSpawnTypeProperties = GetOrDefault(nameof(QuadrantSpawnTypeProperties), QuadrantSpawnTypeProperties);
        ElementsPopulation = GetOrDefault(nameof(ElementsPopulation), ElementsPopulation);
        ManagedElementsPopulation = GetOrDefault(nameof(ManagedElementsPopulation), ManagedElementsPopulation);
        SurvivorItemPopulationProbability = GetOrDefault<FPackageIndex?>(nameof(SurvivorItemPopulationProbability));
        ProceduralTileWidth = GetOrDefault(nameof(ProceduralTileWidth), ProceduralTileWidth);
        ProceduralTileHeight = GetOrDefault(nameof(ProceduralTileHeight), ProceduralTileHeight);
    }
}
