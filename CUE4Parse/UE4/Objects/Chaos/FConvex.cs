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
            // TODO:
            throw new NotImplementedException();
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
            // TODO:
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
        
        if (FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.MarginAddedToConvexAndBox)
            Margin = Ar.Read<float>();

        if (FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.StructureDataAddedToConvex)
        {
            StructureData = new FConvexStructureData(Ar);
        }
        else
        {
            throw new NotImplementedException();
        }

        if (FUE5ReleaseStreamObjectVersion.Get(Ar) >= FUE5ReleaseStreamObjectVersion.Type.AddedInertiaTensorAndRotationOfMassAddedToConvex)
        {
            UnitMassInertiaTensor = Ar.Read<FVector>();
            RotationOfMass = new FQuat(Ar.Read<TIntVector4<double>>());
        }
        else
        {
            throw new NotImplementedException();
        }
    }
}