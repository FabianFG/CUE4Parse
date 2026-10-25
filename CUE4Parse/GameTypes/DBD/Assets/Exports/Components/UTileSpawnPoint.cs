using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Assets.Exports.Components;

public class UTileSpawnPoint : USceneComponent
{
    public ETileSpawnPointType TileSpawnPointType;
    public double Weight = 1;
    public FSoftObjectPath Visualization;
    public int _spawnPriorityTier = 1;
    public bool _weightInfluenceable = true;
    public bool _weightInfluencer = true;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Game is not GAME_DeadByDaylight)
            return;

        TileSpawnPointType = GetOrDefault(nameof(TileSpawnPointType), TileSpawnPointType);
        Weight = GetOrDefault(nameof(Weight), Weight);
        Visualization = GetOrDefault(nameof(Visualization), Visualization);
        _spawnPriorityTier = GetOrDefault(nameof(_spawnPriorityTier), _spawnPriorityTier);
        _weightInfluenceable = GetOrDefault(nameof(_weightInfluenceable), _weightInfluenceable);
        _weightInfluencer = GetOrDefault(nameof(_weightInfluencer), _weightInfluencer);
    }
}
