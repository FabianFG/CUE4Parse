using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DaysGone.Assets.Exports;

public class UCustomBendClass : UObject
{
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        return;
    }
}

public class UBendBlockingVolumeCollectionComponent : UCustomBendClass;
public class UBendDecalCollectionComponent : UCustomBendClass;
public class UBendNavLinkCollectionComponent : UCustomBendClass;
public class UBendStaticMeshCollectionComponent : UCustomBendClass;

public class FBendActorInfo(FAssetArchive Ar)
{
    public string Name = Ar.ReadFString();
    public FVector Position = new(Ar);
    public FPackageIndex Owner = new(Ar);
    public float UnknownFloat = Ar.Read<float>();
    public FPackageIndex[] Unknown = Ar.ReadArray(() => new FPackageIndex(Ar));
    public FPackageIndex[] Navigation = Ar.ReadArray(() => new FPackageIndex(Ar)); // might be something else, cause some are not FPackageIndex
    public FPackageIndex[] OtherActors = Ar.ReadArray(() => new FPackageIndex(Ar));
}
