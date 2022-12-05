using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection
{
    public class FTransformCollection : FManagedArrayCollection
    {
        public FTransformCollection(FAssetArchive Ar) : base(Ar)
        {
            // https://github.com/EpicGames/UnrealEngine/blob/ue5-main/Engine/Source/Runtime/Experimental/Chaos/Private/GeometryCollection/TransformCollection.cpp#L47
        }
    }
}
