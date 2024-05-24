using CUE4Parse.UE4.Assets.Exports.Nanite;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection
{
    public class UGeometryCollection : UObject
    {
        // public FGeometryCollection? GeometryCollection { get; private set; }
        // public FGeometryCollectionNaniteData? NaniteData { get; private set; }
        // public FNaniteResources? OldNaniteData { get; private set; }
        public FPackageIndex[] Materials { get; private set; }
        public FGeometryCollectionProxyMeshData? RootProxyData { get; private set; }

        public override void Deserialize(FAssetArchive Ar, long validPos)
        {
            base.Deserialize(Ar, validPos);
            Materials = GetOrDefault<FPackageIndex[]>(nameof(Materials), []);
            RootProxyData = GetOrDefault<FGeometryCollectionProxyMeshData>(nameof(RootProxyData));

            // var bIsCookedOrCooking = FDestructionObjectVersion.Get(Ar) >= FDestructionObjectVersion.Type.GeometryCollectionInDDC && Ar.ReadBoolean();
            //
            // if (FDestructionObjectVersion.Get(Ar) >= FDestructionObjectVersion.Type.GeometryCollectionInDDCAndAsset)
            // {
            //     GeometryCollection = new FGeometryCollection(Ar);
            // }
            //
            // if (FUE5MainStreamObjectVersion.Get(Ar) == FUE5MainStreamObjectVersion.Type.GeometryCollectionNaniteData ||
            //     (FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.GeometryCollectionNaniteCooked &&
            //      FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.GeometryCollectionNaniteTransient))
            // {
            //     // This legacy version serialized structure information into archive, but the data is transient.
            //     // Just load it and throw away here, it will be rebuilt later and resaved past this point.
            //     OldNaniteData = new FNaniteResources(Ar);
            // }
            //
            // if (FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.GeometryCollectionNaniteTransient)
            // {
            //     var bCooked = Ar.ReadBoolean();
            //     if (bCooked)
            //     {
            //         NaniteData = new FGeometryCollectionNaniteData(Ar);
            //     }
            // }
        }
    }
}
