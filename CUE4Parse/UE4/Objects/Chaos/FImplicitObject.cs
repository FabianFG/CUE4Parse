using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Chaos.Union;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Chaos;

public class FImplicitObject : IChaosClass
{
    public bool bIsConvex { get; set;}
    public bool bDoCollide { get; set; }
    public EImplicitObjectType CollisionType { get; set;}
    public float Margin { get; set;}
    
    public virtual void Serialize(FChaosArchive Ar)
    {
        if (FDestructionObjectVersion.Get(Ar) >= FDestructionObjectVersion.Type.ChaosArchiveAdded)
        {
            bIsConvex = Ar.ReadBoolean();
            bDoCollide = Ar.ReadBoolean();
        }

        if (FDestructionObjectVersion.Get(Ar) <= FDestructionObjectVersion.Type.ImplicitObjectDoCollideAttribute)
        {
            bDoCollide = true;
        }
        
        if (FReleaseObjectVersion.Get(Ar) > FReleaseObjectVersion.Type.CustomImplicitCollisionType)
        {
            CollisionType = Ar.Read<EImplicitObjectType>();
        }
    }

    public static IChaosClass SerializationFactory(FChaosArchive Ar)
    {
        var objectType = Ar.Read<EImplicitObjectType>();
        if (FExternalPhysicsCustomObjectVersion.Get(Ar) >= FExternalPhysicsCustomObjectVersion.Type.ScaledGeometryIsConcrete)
        {
            if (objectType.HasFlag(EImplicitObjectType.IsScaled))
            {
                var innerType = GetInnerType(objectType);
                return innerType switch
                {    
                    EImplicitObjectType.Convex => new TImplicitObjectScaled<FConvex>(),
                    //EImplicitObjectType.TriangleMesh => new TImplicitObjectScaled<FTriangleMesh>();
                    _ => throw new ParserException($"InnerType can't be of {innerType} when ObjectType is scaled.")
                };
            }
        }
        
        if (objectType.HasFlag(EImplicitObjectType.IsInstanced))
        {
            var innerType = GetInnerType(objectType);
            return innerType switch
            {
                EImplicitObjectType.Convex => new TImplicitObjectInstanced<FConvex>(),
                //EImplicitObjectType.TriangleMesh => new TImplicitObjectInstanced<FTriangleMeshImplicitObject>(),
                _ => throw new ParserException($"InnerType can't be of {innerType} when ObjectType is instanced.")
            };
        }

        return objectType switch
        {
            EImplicitObjectType.Sphere => new TSphere(),
            EImplicitObjectType.Box => new TBox<float>(),
            //EImplicitObjectType.Plane,
            EImplicitObjectType.Transformed => new TImplicitObjectTransformed(),
            EImplicitObjectType.Capsule => new FCapsule(),
            EImplicitObjectType.Union => new FImplicitObjectUnion(),
            //EImplicitObjectType.LevelSet,
            //EImplicitObjectType.Unknown,
            EImplicitObjectType.Convex => new FConvex(),
            _ => throw new NotImplementedException($"SerializationFactory for {objectType} is not implemented.")
        };
    }

    private static EImplicitObjectType GetInnerType(EImplicitObjectType type) => type & ~(EImplicitObjectType.IsWeightedLattice | EImplicitObjectType.IsScaled | EImplicitObjectType.IsInstanced);
}

[Flags]
public enum EImplicitObjectType : byte
{
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
    MLLevelSet,
    SkinnedTriangleMesh,
    ExtrudedTaperedCapsule,
    
    //Add entries above this line for serialization
    ConcreteObjectCount, // Used to ensure bitflags do not overlap concrete type
    IsWeightedLattice = 1 << 5,
    IsInstanced = 1 << 6,
    IsScaled = 1 << 7
}