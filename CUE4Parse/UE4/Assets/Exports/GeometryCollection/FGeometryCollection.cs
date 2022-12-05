using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection
{
    public class FGeometryCollection : FTransformCollection
    {
        public FGeometryCollection(FAssetArchive Ar) : base(Ar)
        {
            // https://github.com/EpicGames/UnrealEngine/blob/ue5-main/Engine/Source/Runtime/Experimental/Chaos/Private/GeometryCollection/GeometryCollection.cpp#L993
        }
    }
}
