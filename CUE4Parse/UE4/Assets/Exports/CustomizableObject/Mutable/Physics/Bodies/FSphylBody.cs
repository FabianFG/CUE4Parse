using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Physics.Bodies;

public class FSphylBody : FBodyShape
{
    public FVector Position;
    public FQuat Orientation;
    public float Radius;
    public float Length;

    public FSphylBody(FArchive Ar) : base(Ar)
    {
        var version = Ar.Read<int>();

        Position = Ar.Read<FVector>();
        Orientation = Ar.Read<FQuat>();
        Radius = Ar.Read<float>();
        Length = Ar.Read<float>();
    }
}
