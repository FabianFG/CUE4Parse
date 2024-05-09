using CUE4Parse_Conversion.UEFormat;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.UEFormat.Collision;

public struct FSphereMeshCollision(FKSphereElem SphereElem) : ISerializable
{
    public void Serialize(FArchiveWriter Ar)
    {
        Ar.WriteFString(SphereElem.Name.Text);
        SphereElem.Center.Serialize(Ar);
        Ar.Write(SphereElem.Radius);
    }
}