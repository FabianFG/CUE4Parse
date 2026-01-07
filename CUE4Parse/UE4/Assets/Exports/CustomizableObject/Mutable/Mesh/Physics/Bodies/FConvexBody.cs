using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Physics.Bodies;

public class FConvexBody : FBodyShape
{
    public FVector[] Vertices;
    public int[] Indices;
    public FTransform Transform;

    public FConvexBody(FMutableArchive Ar) : base(Ar)
    {
        Vertices = Ar.ReadArray<FVector>();
        Indices = Ar.ReadArray<int>();
        Transform = Ar.Read<FTransform>();
    }
}