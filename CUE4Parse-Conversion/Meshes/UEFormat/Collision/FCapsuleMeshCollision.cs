using CUE4Parse_Conversion.UEFormat;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.UEFormat.Collision;

public struct FCapsuleMeshCollision(FKSphylElem SphylElem) : ISerializable
{
    public void Serialize(FArchiveWriter Ar)
    {
        Ar.WriteFString(SphylElem.Name.Text);
        SphylElem.Center.Serialize(Ar);
        SphylElem.Rotation.Serialize(Ar);
        Ar.Write(SphylElem.Radius);
        Ar.Write(SphylElem.Length);
    }
}