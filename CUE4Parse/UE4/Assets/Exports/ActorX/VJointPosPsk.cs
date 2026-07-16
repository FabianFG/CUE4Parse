using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.ActorX;

public struct VJointPosPsk
{
    public FQuat Orientation;
    public FVector Position;

    public VJointPosPsk(FArchive Ar)
    {
        Orientation = new FQuat(Ar);
        Position = new FVector(Ar);
    }
}
