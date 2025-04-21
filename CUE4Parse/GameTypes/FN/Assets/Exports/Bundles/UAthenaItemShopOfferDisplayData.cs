using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine;

namespace CUE4Parse.GameTypes.FN.Assets.Exports.Bundles;

public class UAthenaItemShopOfferDisplayData : UPrimaryDataAsset
{
    public FContextualPresentation[] ContextualPresentations;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        ContextualPresentations = GetOrDefault<FContextualPresentation[]>(nameof(ContextualPresentations), []);
    }
}
