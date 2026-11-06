using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.HairStrands;

// used before UE5.3
public class FHairGroupBulkData
{
    public FHairStrandsRootBulkData_Old SimRootBulkDataOld;
    public FHairStrandsRootBulkData_Old? RenRootBulkData;
    public FHairStrandsRootBulkData_Old[]? CardsRootBulkData;

    public FHairGroupBulkData(FAssetArchive Ar, EGroomClassStripFlags flags)
    {
        SimRootBulkDataOld = new FHairStrandsRootBulkData_Old(Ar);
        if (!flags.HasFlag(EGroomClassStripFlags.CDSF_ImportedStrands))
        {
            RenRootBulkData = new FHairStrandsRootBulkData_Old(Ar);
        }

        if (FAnimObjectVersion.Get(Ar) >= FAnimObjectVersion.Type.SerializeHairBindingAsset)
        {
            CardsRootBulkData = Ar.ReadArray(() => new FHairStrandsRootBulkData_Old(Ar));
        }
    }
}
