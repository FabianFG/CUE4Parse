using CUE4Parse_Conversion.UEFormat;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.UEFormat.Collision;

public struct FTaperedCapsuleMeshCollision(FKTaperedCapsuleElem TaperedCapsuleElem) : ISerializable
{
    public void Serialize(FArchiveWriter Ar)
    {
        Ar.WriteFString(TaperedCapsuleElem.Name.Text);
        TaperedCapsuleElem.Center.Serialize(Ar);
        TaperedCapsuleElem.Rotation.Serialize(Ar);
        Ar.Write(TaperedCapsuleElem.Radius0);
        Ar.Write(TaperedCapsuleElem.Radius1);
        Ar.Write(TaperedCapsuleElem.Length);
    }
}