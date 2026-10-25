using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.OtherGames.Objects;

public class AGeneratedMeshActor : AActor
{
    public Dictionary<EMeshType, FPackageIndex> ComponentsSplit = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        ComponentsSplit = GetOrDefault(nameof(ComponentsSplit), ComponentsSplit);
    }

    public enum EMeshType : byte
    {
        Terrain = 0,
        Leaf = 1,
        Water = 2,
        OuchWater = 3,
        Lava = 4,
        Kill = 5,
        Sculk = 6,
        VoidFill = 7,
        ShadowTerrain = 8,
        ShadowLeaf = 9,
        ShadowWater = 10,
        ShadowLava = 11,
        ShadowSculk = 12,
        TerrainFill = 13,
        Count = 14,
    }
}

public class UGeneratedMeshActorComponentSplit : USceneComponent
{
    public FPackageIndex[] SubMeshes = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        SubMeshes = GetOrDefault(nameof(SubMeshes), SubMeshes);
    }
}
