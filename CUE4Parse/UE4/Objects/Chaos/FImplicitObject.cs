using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Chaos;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.Chaos;


[Flags]
public enum EImplicitObjectType: byte
{
    //Note: add entries in order to avoid serialization issues (but before IsInstanced)
    Sphere = 0, // warning: code assumes that this is an FSphere, but all TSpheres will think this is their type.
    Box,
    Plane,
    Capsule,
    Transformed,
    Union,
    LevelSet,
    Unknown,
    Convex,
    TaperedCylinder,
    Cylinder,
    TriangleMesh,
    HeightField,
    DEPRECATED_Scaled,	//needed for serialization of existing data
    Triangle,
    UnionClustered,
    TaperedCapsule,
    WeightedLatticeBone,

    //Add entries above this line for serialization
    ConcreteObjectCount, // Used to ensure bitflags do not overlap concrete type
    IsWeightedLattice = 1 << 5,
    IsInstanced = 1 << 6,
    IsScaled = 1 << 7
}

public struct FReal
{
    public double Value;

    public FReal(FAssetArchive Ar)
    {
        if (Ar.Ver >= EUnrealEngineObjectUE5Version.LARGE_WORLD_COORDINATES)
        {
            Value = Ar.Read<float>();
        }
        else
        {
            Value = Ar.Read<double>();
        }
    }

    public FReal(double value)
	{
		Value = value;
	}
}

public class FImplicitObject: ISerializationFactory
{
    public FReal Margin;
    public bool bIsConvex;
    public bool bDoCollide;
    public bool bHasBoundingBox;


    public EImplicitObjectType Type;
    public EImplicitObjectType CollisionType;

    public FImplicitObject()
    {

    }

    public virtual ISerializationFactory Serialize(FChaosArchive Ar)
    {
        if (FDestructionObjectVersion.Get(Ar) >= FDestructionObjectVersion.Type.ChaosArchiveAdded)
        {
            bIsConvex = Ar.ReadBoolean();
            bDoCollide = Ar.ReadBoolean();
        }

        if (FDestructionObjectVersion.Get(Ar) >= FDestructionObjectVersion.Type.ImplicitObjectDoCollideAttribute)
        {
            bDoCollide = true;
        }

        if (FReleaseObjectVersion.Get(Ar) > FReleaseObjectVersion.Type.CustomImplicitCollisionType)
        {
            CollisionType = (EImplicitObjectType) Ar.Read<byte>();
        }

        return this;
    }

    public virtual ISerializationFactory SerializationFactory(FChaosArchive Ar)
    {
        EImplicitObjectType objectType = Ar.Read<EImplicitObjectType>(); // because Ar.Loading

        if (FExternalPhysicsCustomObjectVersion.Get(Ar) >= FExternalPhysicsCustomObjectVersion.Type.ScaledGeometryIsConcrete)
        {
            if (IsScaled(objectType)) {
                EImplicitObjectType innerType = GetInnerType(objectType);

                switch (innerType)
                {
                    case EImplicitObjectType.Convex:
                        return new FImplicitObject();
                    default:
	                   throw new ParserException("Unknown scaled geometry type " + innerType);
                }
            }
        }

        if (IsInstanced(objectType))
        {
            EImplicitObjectType innerType = GetInnerType(objectType);

            switch (innerType)
            {
                case EImplicitObjectType.Convex:
                    return new TImplicitObjectInstanced<FConvex>();

                default:
	                throw new ParserException("Unknown instanced geometry type " + innerType);
            }
        }

        switch (objectType)
        {
            case EImplicitObjectType.Union:
	            return new FImplicitObjectUnion();
			case EImplicitObjectType.Convex:
				return new FConvex();
			case EImplicitObjectType.Transformed:
				return new TImplicitObjectTransformed<FReal>();
            default:
                throw new ParserException("Unknown ImplicitObjectType " + objectType);
        }

        return this;
    }

    bool IsScaled(EImplicitObjectType type)
    {
        return (type & EImplicitObjectType.IsScaled) != 0;
    }

    bool IsInstanced(EImplicitObjectType type)
    {
        return (type & EImplicitObjectType.IsInstanced) != 0;
    }

    EImplicitObjectType GetInnerType(EImplicitObjectType Type)
    {
        return Type & (~(EImplicitObjectType.IsWeightedLattice | EImplicitObjectType.IsScaled | EImplicitObjectType.IsInstanced));
    }
}

public class TBox<T> : FImplicitObject where T: struct
{
	private TAABB<T> AABB;

	public TBox(int dimensions) : base()
	{
		AABB = new TAABB<T>(dimensions, default(T));
		Type = EImplicitObjectType.Box;
	}

	public override ISerializationFactory Serialize(FChaosArchive Ar)
	{
		base.Serialize(Ar);
		AABB.Serialize(Ar);

		if (FReleaseObjectVersion.Get(Ar) >= FReleaseObjectVersion.Type.MarginAddedToConvexAndBox)
		{
			// FRealSingle
			Margin = new FReal(Ar.Read<float>());
		}
		return this;
	}

	public static TAABB<T> SerializeAsAABB(FChaosArchive Ar, int dimensions)
	{
		if (FExternalPhysicsCustomObjectVersion.Get(Ar) < FExternalPhysicsCustomObjectVersion.Type.TBoxReplacedWithTAABB)
		{
			var box = new TBox<T>(dimensions);
			box.Serialize(Ar);
			return box.AABB;
		}

		return new TAABB<T>(Ar, dimensions);
	}

	public static Dictionary<int, TAABB<T>> SerializeAsAABBs(FChaosArchive Ar, int dimensions)
	{
		var count = Ar.Read<int>();
		var AABBs = new Dictionary<int, TAABB<T>>();
		AABBs.EnsureCapacity(count);

		for (int i = 0; i < count; i++)
		{
			var key =  Ar.Read<int>();
			// var val = Ar.ReadArray(() => new TAABB<T>(Ar, dimensions));
			// var val = Ar.ReadArray(() => SerializeAsAABB(Ar, dimensions));
			var val = SerializeAsAABB(Ar, dimensions);
			AABBs[key] = val;
		}

		return AABBs;
	}
}

public class FImplicitObjectUnion : FImplicitObject
{
	private FImplicitObject[] MObjects;

	private FAABB3 MLocalBoundingBox;

    private int NumLeafObjects;

	public FImplicitObjectUnion() : base()
	{
		Type = EImplicitObjectType.Union;
	}

	public override ISerializationFactory Serialize(FChaosArchive Ar)
	{
		base.Serialize(Ar);

		MObjects = Ar.SerializePtrArray(() => new FImplicitObject());

		MLocalBoundingBox = new FAABB3();

		if (Ar.Game == EGame.GAME_MarvelRivals)
		{
			var aabb = TBox<float>.SerializeAsAABB(Ar, 3);
			MLocalBoundingBox.Min = new TVector<double>(aabb.Min[0], aabb.Min[1], aabb.Min[2]);
			MLocalBoundingBox.Max = new TVector<double>(aabb.Max[0], aabb.Max[1], aabb.Max[2]);
		}
		else
		{
			var aabb = TBox<float>.SerializeAsAABB(Ar, 3); // ue 5.4
			MLocalBoundingBox.Min = new TVector<double>(aabb.Min[0], aabb.Min[1], aabb.Min[2]);
			MLocalBoundingBox.Max = new TVector<double>(aabb.Max[0], aabb.Max[1], aabb.Max[2]);
		}

		bool bHierarchyBuilt;
        if (FExternalPhysicsCustomObjectVersion.Get(Ar) < FExternalPhysicsCustomObjectVersion.Type.UnionObjectsCanAvoidHierarchy)
        {
	        // LegacySerializeBVH(Ar);
	        // bHierarchyBuilt = Ar.ReadBoolean();
        }
        else if (FFortniteMainBranchObjectVersion.Get(Ar) < FFortniteMainBranchObjectVersion.Type.ChaosImplicitObjectUnionBVHRefactor)
        {
	        // bHierarchyBuilt = Ar.ReadBoolean();
	        // if (bHierarchyBuilt) LegacySerializeBVH(Ar);
        }
        else
        {
            var flagsBit = Ar.Read<byte>(); // FFLags union of bAllowBVH 1 and bHasBVH 1

            if (FFortniteSeasonBranchObjectVersion.Get(Ar) < FFortniteSeasonBranchObjectVersion.Type.ChaosImplicitObjectUnionLeafObjectsToInt32)
            {
                NumLeafObjects = (int)Ar.Read<ushort>();
            }
            else
            {
                NumLeafObjects = Ar.Read<int>();
            }


            // var allowBVH = (flagsBit & 1) != 0;
            var hasBVH = (flagsBit & 2) != 0;
            if (hasBVH)
            {
                // FImplicitBVH
	            var bvh = new FImplicitBVH(Ar);
            }

        }

		return this;
	}
}

public class TImplicitObjectTransformed<T>: FImplicitObject
{
	private FImplicitObject MObject;
	private FTransform MTransform; // TRigidTransform<T, d>
	private FAABB3 MLocalBoundingBox; // TAABB<T, d>

	private const int d = 3;

	public override ISerializationFactory Serialize(FChaosArchive Ar)
	{
		base.Serialize(Ar);

		MObject = Ar.SerializePtr(new FImplicitObject());
		MTransform = Ar.Read<TTransform<double>>(); // FReal


		if (Ar.Game == EGame.GAME_MarvelRivals)
		{
			var aabb = TBox<float>.SerializeAsAABB(Ar, 3);
			// MLocalBoundingBox = new FAABB3(aabb);

		}
		else
		{
			var aabb = TBox<double>.SerializeAsAABB(Ar, 3);
			MLocalBoundingBox = new FAABB3(aabb);
		}

		return this;
	}
}
