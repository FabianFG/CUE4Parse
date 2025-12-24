using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.DataAssets;

public class UAthenaPickaxeItemDefinition : UAthenaCosmeticItemDefinition
{
    public FPackageIndex WeaponDefinition;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        WeaponDefinition = GetOrDefault(nameof(WeaponDefinition), new FPackageIndex());
    }
}
