using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;

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

