using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Groom;

public class FHairGroupData
{
    public FHairStrandsRootData SimRootData;
    public FHairStrandsRootData RenRootData;
    public FHairStrandsRootData[]? CardsRootData;

    public FHairGroupData(FAssetArchive Ar)
    {
        SimRootData = new FHairStrandsRootData(Ar);
        RenRootData = new FHairStrandsRootData(Ar);

        if (FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.SerializeHairBindingAsset)
        {
            CardsRootData = Ar.ReadArray(() => new FHairStrandsRootData(Ar));
        }
    }
}
