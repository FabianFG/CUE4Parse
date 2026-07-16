using CUE4Parse.UE4.Assets.Exports.Chaos;

namespace CUE4Parse.UE4.Assets.Exports.GeometryCollection
{
    public class FGeometryCollection : FTransformCollection
    {
        public FGeometryCollection(FChaosArchive Ar) : base(Ar)
        {
            //https://github.com/EpicGames/UnrealEngine/blob/243b17aec84366dc66df6069e0b10e734d5f1e9b/Engine/Source/Runtime/Experimental/Chaos/Private/GeometryCollection/GeometryCollection.cpp#L1106
        }
    }
}
