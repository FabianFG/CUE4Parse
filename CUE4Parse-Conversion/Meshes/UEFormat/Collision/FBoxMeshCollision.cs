using CUE4Parse_Conversion.UEFormat;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.UEFormat.Collision;

public struct FBoxMeshCollision(FKBoxElem BoxElem) : ISerializable
{
    public void Serialize(FArchiveWriter Ar)
    {
        Ar.WriteFString(BoxElem.Name.Text);
        BoxElem.Center.Serialize(Ar);
        BoxElem.Rotation.Serialize(Ar);
        Ar.Write(BoxElem.X);
        Ar.Write(BoxElem.Y);
        Ar.Write(BoxElem.Z);
    }
}