using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Groom;

public class FHairGroupBulkData
{
    public FHairStrandsRootBulkData SimRootBulkData;
    public FHairStrandsRootBulkData? RenRootBulkData;
    public FHairStrandsRootBulkData[]? CardsRootBulkData;

    public FHairGroupBulkData(FAssetArchive Ar, uint flags)
    {
        SimRootBulkData = new FHairStrandsRootBulkData(Ar);
        if ((flags & UGroomBindingAsset.CDSF_StrandsStripped) == 0)
        {
            RenRootBulkData = new FHairStrandsRootBulkData(Ar);
        }

        if (FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.SerializeHairBindingAsset)
        {
            CardsRootBulkData = Ar.ReadArray(() => new FHairStrandsRootBulkData(Ar));
        }
    }
}
