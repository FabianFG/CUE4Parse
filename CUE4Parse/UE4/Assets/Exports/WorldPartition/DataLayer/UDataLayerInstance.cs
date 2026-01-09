using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.WorldPartition.DataLayer;

public class UDataLayerInstance : UObject
{
    public EDataLayerRuntimeState InitialRuntimeState;
    public FPackageIndex Parent;
    public FPackageIndex[] Children;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        InitialRuntimeState = GetOrDefault<EDataLayerRuntimeState>(nameof(InitialRuntimeState));
        Parent = GetOrDefault<FPackageIndex>(nameof(Parent));
        Children = GetOrDefault<FPackageIndex[]>(nameof(Children), []);
    }
}
