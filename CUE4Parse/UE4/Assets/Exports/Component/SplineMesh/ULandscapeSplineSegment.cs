using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;

public class ULandscapeSplineSegment : UObject
{
    public FPackageIndex?[] LocalMeshComponents = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        LocalMeshComponents = GetOrDefault(nameof(LocalMeshComponents), LocalMeshComponents);
    }
}
