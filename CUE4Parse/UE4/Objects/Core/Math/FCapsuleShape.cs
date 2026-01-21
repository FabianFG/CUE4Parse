using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.Core.Math;

public struct FCapsuleShape(FAssetArchive Ar) : IUStruct
{
    /** The capsule's center point. */
    public FVector Center = new FVector(Ar);
    /** The capsule's radius. */
    float Radius = Ar.ReadFReal();
    /** The capsule's orientation in space. */
    FVector Orientation = new FVector(Ar);
    /** The capsule's length. */
    float Length = Ar.ReadFReal();
}
