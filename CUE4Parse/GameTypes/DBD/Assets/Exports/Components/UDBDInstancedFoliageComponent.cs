using CUE4Parse.GameTypes.DBD.Objects.Foliage;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Foliage;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Assets.Exports.Components;

public class UDBDInstancedFoliageComponent : UActorComponent
{
    public FDBDFoliageInfo[] Foliage = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Game is not GAME_DeadByDaylight)
            return;

        Foliage = GetOrDefault(nameof(Foliage), Foliage);
    }

    public IEnumerable<(FPackageIndex Mesh, FPackageIndex?[] Materials, FTransform[] Transforms)> GetMeshes()
    {
        foreach (var foliage in Foliage)
        {
            if (foliage.Instances.Length == 0 || foliage.FoliageType?.TryLoad<UFoliageType>(out var type) != true || type?.GetOrDefault<FPackageIndex?>("Mesh") is not { IsNull: false } mesh)
                continue;

            yield return (mesh, type.GetOrDefault<FPackageIndex?[]>("OverrideMaterials", []), [.. foliage.Instances.Select(instance => instance.GetTransform())]);
        }
    }
}
