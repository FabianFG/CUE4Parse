using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.Nascar.Assets.Exports;

public class UIRMeshComponent : UMeshComponent
{
    public FPackageIndex Mesh { get; private set; } = new();

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        Mesh = GetOrDefault(nameof(Mesh), Mesh);
    }
}
