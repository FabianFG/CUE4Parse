using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.FastGeoStreaming;

public class FFastGeoMeshComponent : FFastGeoPrimitiveComponent
{
    public FPackageIndex[] OverrideMaterials;

    public FFastGeoMeshComponent(FFastGeoArchive Ar) : base(Ar)
    {
        OverrideMaterials = Ar.ReadArray(Ar.ReadFPackageIndex);
    }
}
