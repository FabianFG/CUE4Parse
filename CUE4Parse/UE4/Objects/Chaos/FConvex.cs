using CUE4Parse.UE4.Objects.Chaos.Convex;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Chaos;

public sealed class FConvex : FImplicitObject
{
    public TCorePlane<float>[] Planes { get; set; }
    public FVector[] Vertices { get; set; }
    public TAABB<float> LocalBoundingBox { get; set; }
    public float Volume { get; set; }
    public FVector CentreOfMass { get; set; }
    public FConvexStructureData StructureData { get; set; }
    public FVector UnitMassInertiaTensor { get; set; }
    public FQuat RotationOfMass { get; set; }

    public override void Serialize(FChaosArchive Ar)
    {
        base.Serialize(Ar);

        if (FExternalPhysicsCustomObjectVersion.Get(Ar) < FExternalPhysicsCustomObjectVersion.Type.ConvexUsesTPlaneConcrete)
        {
            var tempPlane = Ar.ReadPtr<TPlane<float>>();
        }
        else
        {
            Planes = Ar.ReadArray(() => new TCorePlane<float>(Ar, 3));
        }

        var bConvexVerticesNewFormatUE4 = FPhysicsObjectVersion.Get(Ar) >= FPhysicsObjectVersion.Type.ConvexUsesVerticesArray;
        var bConvexVerticesNewFormatUE5 = FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.ConvexUsesVerticesArray;
        var bConvexVerticesNewFormatFN = FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.ChaosConvexVariableStructureDataAndVerticesArray;
        var bConvexVerticesNewFormat = bConvexVerticesNewFormatUE4 || bConvexVerticesNewFormatUE5 || bConvexVerticesNewFormatFN;

        if (!bConvexVerticesNewFormat)
        {
            //var tmpSurfaceParticles = Ar.ReadPtr<FParticles>();
            // https://github.com/EpicGames/UnrealEngine/blob/71fe36aac5a8df5ccd66c763ffc902b29b6a9c43/Engine/Source/Runtime/Experimental/Chaos/Public/Chaos/Convex.h#L953
        }
        else
        {
            Vertices = Ar.ReadArray<FVector>();
        }

        LocalBoundingBox = TBox<float>.SerializeAsAABB(Ar, 3);

        if (FExternalPhysicsCustomObjectVersion.Get(Ar) >= FExternalPhysicsCustomObjectVersion.Type.AddConvexCenterOfMassAndVolume)
        {
            Volume = Ar.Read<float>();
            CentreOfMass = Ar.Read<FVector>();
        }
        else
        {
            // UE 4.24-
            // https://github.com/EpicGames/UnrealEngine/blob/71fe36aac5a8df5ccd66c763ffc902b29b6a9c43/Engine/Source/Runtime/Experimental/Chaos/Public/Chaos/Convex.h#L985-L992
        }

        if (FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.MarginAddedToConvexAndBox)
            Margin = Ar.Read<float>();

        if (FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.StructureDataAddedToConvex)
        {
            StructureData = new FConvexStructureData(Ar);
        }
        else
        {
            // UE 4.25-
            // https://github.com/EpicGames/UnrealEngine/blob/71fe36aac5a8df5ccd66c763ffc902b29b6a9c43/Engine/Source/Runtime/Experimental/Chaos/Public/Chaos/Convex.h#L1009
        }

        if (FUE5ReleaseStreamObjectVersion.Get(Ar) >= FUE5ReleaseStreamObjectVersion.Type.AddedInertiaTensorAndRotationOfMassAddedToConvex)
        {
            UnitMassInertiaTensor = Ar.Read<FVector>();
            RotationOfMass = new FQuat(Ar.Read<TIntVector4<double>>());
        }
        else
        {
            // UE 5.0EA-
            //ComputeUnitMassInertiaTensorAndRotationOfMass(Volume);
        }
    }
}
