using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Landscape;

public class ULandscapeNaniteComponent : UStaticMeshComponent
{
    public FGuid? ProxyContentId { get; private set; }
    public bool bEnabled { get; private set; } = true;
    public FPackageIndex?[]? SourceLandscapeComponents { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        ProxyContentId = GetOrDefault<FGuid?>(nameof(ProxyContentId));
        bEnabled = GetOrDefault(nameof(bEnabled), bEnabled);
        SourceLandscapeComponents = GetOrDefault<FPackageIndex?[]?>(nameof(SourceLandscapeComponents));
    }
}
