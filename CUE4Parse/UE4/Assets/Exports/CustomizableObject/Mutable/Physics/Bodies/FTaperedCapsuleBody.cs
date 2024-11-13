using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Physics.Bodies;

public class FTaperedCapsuleBody : FBodyShape
{
    public FVector Position;
    public FQuat Orientation;
    public float Radius0;
    public float Radius1;
    public float Length;

    public FTaperedCapsuleBody(FArchive Ar) : base(Ar)
    {
        var version = Ar.Read<int>();

        Position = Ar.Read<FVector>();
        Orientation = Ar.Read<FQuat>();
        Radius0 = Ar.Read<float>();
        Radius1 = Ar.Read<float>();
        Length = Ar.Read<float>();
    }
}
