using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Assets.Exports.Components;

public class UActorSpawner : USceneComponent
{
    public EGameplayElementType Type;
    public FSoftObjectPath Visualization;
    public bool ActivatedByDefault = true;
    public double Weight = 1;
    public int SpawnCountMax = 1;
    public int SpawnPriorityTier = 1;
    public bool WeightInfluenceable = true;
    public bool WeightInfluencer = true;
    public float Cost = 1;
    public FName[] ComponentTags = [];
    public FActorSpawnerProperties[] ActivatedSceneElement = [];
    public FActorSpawnerProperties[] DeactivatedSceneElement = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Game is not GAME_DeadByDaylight)
            return;

        Type = GetOrDefault(nameof(Type), Type);
        Visualization = GetOrDefault(nameof(Visualization), Visualization);
        ActivatedByDefault = GetOrDefault(nameof(ActivatedByDefault), ActivatedByDefault);
        Weight = GetOrDefault(nameof(Weight), Weight);
        SpawnCountMax = GetOrDefault(nameof(SpawnCountMax), SpawnCountMax);
        SpawnPriorityTier = GetOrDefault(nameof(SpawnPriorityTier), SpawnPriorityTier);
        WeightInfluenceable = GetOrDefault(nameof(WeightInfluenceable), WeightInfluenceable);
        WeightInfluencer = GetOrDefault(nameof(WeightInfluencer), WeightInfluencer);
        Cost = GetOrDefault(nameof(Cost), Cost);
        ComponentTags = GetOrDefault(nameof(ComponentTags), ComponentTags);
        ActivatedSceneElement = GetOrDefault(nameof(ActivatedSceneElement), ActivatedSceneElement);
        DeactivatedSceneElement = GetOrDefault(nameof(DeactivatedSceneElement), DeactivatedSceneElement);
    }
}
