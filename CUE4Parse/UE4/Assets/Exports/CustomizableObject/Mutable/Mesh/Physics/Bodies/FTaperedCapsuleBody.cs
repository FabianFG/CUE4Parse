using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Physics.Bodies;

public class FTaperedCapsuleBody : FBodyShape
{
    public FVector Position;
    public FQuat Orientation;
    public float Radius0;
    public float Radius1;
    public float Length;
    
    public FTaperedCapsuleBody(FMutableArchive Ar) : base(Ar)
    {
        Position = Ar.Read<FVector>();
        Orientation = Ar.Read<FQuat>();
        Radius0 = Ar.Read<float>();
        Radius1 = Ar.Read<float>();
        Length = Ar.Read<float>();
    }
}