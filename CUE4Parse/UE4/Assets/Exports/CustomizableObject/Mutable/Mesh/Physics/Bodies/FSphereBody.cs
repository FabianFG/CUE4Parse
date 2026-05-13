using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Mesh.Physics.Bodies;

public class FSphereBody : FBodyShape
{
    public FVector Position;
    public float Radius;

    public FSphereBody(FMutableArchive Ar) : base(Ar)
    {
        if (Ar.Game < EGame.GAME_UE5_6) Ar.Position += 4;
        Position = Ar.Read<FVector>();
        Radius = Ar.Read<float>();
    }
}
