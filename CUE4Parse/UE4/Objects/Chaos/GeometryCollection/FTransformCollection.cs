using CUE4Parse.UE4.Assets.Exports.Chaos;
using CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection
{
    public class FTransformCollection : FManagedArrayCollection
    {
        public FTransformCollection(FChaosArchive Ar) : base(Ar)
        {
            // https://github.com/EpicGames/UnrealEngine/blob/ue5-main/Engine/Source/Runtime/Experimental/Chaos/Private/GeometryCollection/TransformCollection.cpp#L47
        }
    }
}
