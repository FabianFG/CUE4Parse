using CUE4Parse.UE4.Assets.Exports.Chaos;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using FVec3 = CUE4Parse.UE4.Objects.Core.Math.TIntVector3<double>;
using FRotation3 = CUE4Parse.UE4.Objects.Core.Math.TIntVector4<double>;

namespace CUE4Parse.UE4.Objects.Chaos;

public class TPlaneConcrete<T>
{
    TIntVector3<T>  MX;
    TIntVector3<T>  MNormal;

    public TPlaneConcrete(FArchive Ar)
    {
        MX = Ar.Read<TIntVector3<T>>();
        MNormal = Ar.Read<TIntVector3<T>>();
    }
}


public class TPlane<T>: FImplicitObject where T: struct
{
    public TPlaneConcrete<T> MPlaneConcrete;
    public T Distance;

    public TPlane(int dimension)
    {
        Distance = default;
    }

    public override ISerializationFactory Serialize(FChaosArchive Ar)
    {
        base.Serialize(Ar);
        MPlaneConcrete = new TPlaneConcrete<T>(Ar);
        return this;
    }
}

// FRealType = FRealSingle = float
public class FConvex: FImplicitObject
{

    // FRealType FRealSingle float
    // TPlaneConcrete<FRealType, 3>
    public TPlaneConcrete<float>[] Planes;
    public TIntVector3<float>[] Vertices;
    public TAABB<float> LocalBoundingBox;
    public float Volume;
    public TIntVector3<float> CenterOfMass;
    public float Margin;
    public FConvexStructureData StructureData;
    public FVec3 UnitMassInertiaTensor;
    FRotation3 RotationOfMass;

    public FConvex()
    {}

    public override ISerializationFactory Serialize(FChaosArchive Ar)
    {
        base.Serialize(Ar);

        if (FExternalPhysicsCustomObjectVersion.Get(Ar) < FExternalPhysicsCustomObjectVersion.Type.ConvexUsesTPlaneConcrete)
        {
            // not tested
            var tempPlane = new TPlane<FReal>(3);
            Ar.SerializePtr<TPlane<FReal>>(tempPlane);
            // tempPlane.Serialize(Ar);
        }
        else
        {
            Planes = Ar.ReadArray(() => new TPlaneConcrete<float>(Ar));
        }

        bool bConvexVerticesNewFormatUE4 = FPhysicsObjectVersion.Get(Ar) >= FPhysicsObjectVersion.Type.ConvexUsesVerticesArray;
        bool bConvexVerticesNewFormatUE5 = FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.ConvexUsesVerticesArray;
        bool bConvexVerticesNewFormatFn = FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.ChaosConvexVariableStructureDataAndVerticesArray;
        bool bConvexVerticesNewFormat = bConvexVerticesNewFormatUE4 || bConvexVerticesNewFormatUE5 || bConvexVerticesNewFormatFn;


        if (!bConvexVerticesNewFormat)
        {
            // TODO
            throw new NotImplementedException("Convex vertices old format is not implemented");
        }
        else
        {
            Vertices = Ar.ReadArray<TIntVector3<float>>();
        }

        LocalBoundingBox = TBox<float>.SerializeAsAABB(Ar, 3);

        if (FExternalPhysicsCustomObjectVersion.Get(Ar) >= FExternalPhysicsCustomObjectVersion.Type.AddConvexCenterOfMassAndVolume)
        {
            Volume = Ar.Read<float>();

            CenterOfMass = Ar.Read<TIntVector3<float>>();
        }

        if (FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.MarginAddedToConvexAndBox)
        {
            Margin = Ar.Read<float>(); // FRealSingle
        }

        if (FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.StructureDataAddedToConvex)
        {

            StructureData = new FConvexStructureData(Ar);
        }

        if (FUE5ReleaseStreamObjectVersion.Get(Ar) >= FUE5ReleaseStreamObjectVersion.Type.AddedInertiaTensorAndRotationOfMassAddedToConvex)
        {

            var temp = Ar.Read<TIntVector3<float>>(); // Ar.Read<FVec3>(); says it's double but serializer serialises as float /s
            UnitMassInertiaTensor =  new FVec3(temp.X, temp.Y, temp.Z);

            RotationOfMass = Ar.Read<FRotation3>();
        }

        return this;
    }
}
