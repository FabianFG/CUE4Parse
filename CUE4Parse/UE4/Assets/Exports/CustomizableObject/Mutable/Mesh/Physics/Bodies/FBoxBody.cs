using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Physics.Bodies;

public class FBoxBody : FBodyShape
{
    public FVector Position;
    public FQuat Orientation;
    public FVector Size;

    public FBoxBody(FMutableArchive Ar) : base(Ar)
    {
        Position = Ar.Read<FVector>();
        Orientation = Ar.Read<FQuat>();
        Size = Ar.Read<FVector>();
    }
}