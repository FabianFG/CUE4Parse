using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Physics.Bodies;

public class FSphylBody : FBodyShape
{
    public FVector Position;
    public FQuat Orientation;
    public float Radius;
    public float Length;
    
    public FSphylBody(FMutableArchive Ar) : base(Ar)
    {
        Position = Ar.Read<FVector>();
        Orientation = Ar.Read<FQuat>();
        Radius = Ar.Read<float>();
        Length = Ar.Read<float>();
    }
}