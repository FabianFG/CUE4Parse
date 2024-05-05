using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Meshes.UEFormat.Collision;
using CUE4Parse_Conversion.UEFormat;
using CUE4Parse_Conversion.UEFormat.Structs;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Writers;

namespace CUE4Parse_Conversion.Meshes.UEFormat;

public readonly struct FBodySetup(USkeletalBodySetup BodySetup) : ISerializable
{
    public void Serialize(FArchiveWriter Ar)
    {
        Ar.WriteFString(BodySetup.BoneName.Text);
        Ar.Write((byte) BodySetup.PhysicsType);

        var aggGeom = BodySetup.AggGeom!;
        Ar.WriteArray(aggGeom.SphereElems.Select(elem => new FSphereMeshCollision(elem)));
        Ar.WriteArray(aggGeom.BoxElems.Select(elem => new FBoxMeshCollision(elem)));
        Ar.WriteArray(aggGeom.SphylElems.Select(elem => new FCapsuleMeshCollision(elem)));
        Ar.WriteArray(aggGeom.TaperedCapsuleElems.Select(elem => new FTaperedCapsuleMeshCollision(elem)));
        Ar.WriteArray(aggGeom.ConvexElems.Select(elem => new FConvexMeshCollision(elem)));
    }
}