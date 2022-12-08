using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection
{
    public class FGeometryCollectionNaniteData
    {
        public FNaniteResources Resources { get; private set; }

        public FGeometryCollectionNaniteData(FAssetArchive Ar)
        {
            Resources = new FNaniteResources(Ar);
        }
    }
}
