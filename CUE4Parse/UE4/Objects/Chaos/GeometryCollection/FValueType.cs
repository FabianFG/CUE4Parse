using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public readonly struct FValueType
{
    public readonly EManagedArrayType ArrayType;
    public readonly FName GroupIndexDependency;
    public readonly bool bPersistent;
    public readonly FManagedArrayBase ManagedArray;
    
    public FValueType(FChaosArchive Ar)
    {
        var serializationVersion = Ar.Read<int>();
        if (serializationVersion > 4) throw new ParserException(Ar, $"FValueType Serialization Version ({serializationVersion}) > 4");

        var arrayTypeAsInt = Ar.Read<int>();
        ArrayType = (EManagedArrayType) arrayTypeAsInt;

        if (serializationVersion < 4)
            Ar.Position += sizeof(int); // ArrayScopeAsInt

        if (serializationVersion >= 2)
        {
            GroupIndexDependency = Ar.ReadFName();
            bPersistent = Ar.ReadBoolean();
        }

        ManagedArray = CreateManagedArray(Ar);
        
        var bNewSavedBehaviour = FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.ManagedArrayCollectionAlwaysSerializeValue;
        if (bNewSavedBehaviour || bPersistent)
        {
            ManagedArray.Serialize(Ar);
        }
    }
    
    private FManagedArrayBase CreateManagedArray(FChaosArchive Ar)
    {
        return ArrayType switch
        {
            // EManagedArrayType.FNoneType => expr,
            EManagedArrayType.Vector => new TManagedArray<FVector>(Ar.Read<FVector>, false),
            EManagedArrayType.IntVector => new TManagedArray<FIntVector>(Ar.Read<FIntVector>, false),
            EManagedArrayType.Vector2D => new TManagedArray<FVector2D>(Ar.Read<FVector2D>, false),
            EManagedArrayType.LinearColor => new TManagedArray<FLinearColor>(Ar.Read<FLinearColor>),
            EManagedArrayType.Int32 => new TManagedArray<int>(Ar.Read<int>, false),
            EManagedArrayType.Bool => new TManagedArray<bool>(Ar.ReadFlag, false),
            EManagedArrayType.Transform => new TManagedArray<FTransform>(() => new FTransform(Ar)),
            EManagedArrayType.String => new TManagedArray<string>(Ar.ReadFString),
            EManagedArrayType.Float => new TManagedArray<float>(Ar.Read<float>, false),
            EManagedArrayType.Quat => new TManagedArray<FQuat>(Ar.Read<FQuat>, false),
            EManagedArrayType.BoneNode => new TManagedArray<FGeometryCollectionBoneNode>(() => new FGeometryCollectionBoneNode(Ar)),
            EManagedArrayType.MeshSection => new TManagedArray<FGeometryCollectionSection>(Ar.Read<FGeometryCollectionSection>),
            EManagedArrayType.Box => new TManagedArray<FBox>(() => new FBox(Ar)),
            EManagedArrayType.IntArray => new TManagedArray<int[]>(Ar.ReadArray<int>), // This should be a TSet/HashSet
            EManagedArrayType.Guid => new TManagedArray<FGuid>(Ar.Read<FGuid>, false),
            EManagedArrayType.UInt8 => new TManagedArray<byte>(Ar.Read<byte>, false),
            // EManagedArrayType.VectorArrayPointer => expr,
            // EManagedArrayType.VectorArrayUniquePointer => expr,
            EManagedArrayType.FImplicitObject3Pointer => new TManagedArray<FImplicitObject?>(Ar.ReadPtr<FImplicitObject>),
            // EManagedArrayType.FImplicitObject3UniquePointer => expr,
            // EManagedArrayType.FImplicitObject3SerializablePtr => expr,
            // EManagedArrayType.FBVHParticlesFloat3Pointer => expr,
            // EManagedArrayType.FBVHParticlesFloat3UniquePointer => expr,
            // EManagedArrayType.TPBDRigidParticleHandle3fPtr => expr,
            // EManagedArrayType.TPBDGeometryCollectionParticleHandle3fPtr => expr,
            // EManagedArrayType.TGeometryParticle3fUniquePtr => expr,
            // EManagedArrayType.FImplicitObject3ThreadSafeSharedPointer => expr,
            // EManagedArrayType.FImplicitObject3SharedPointer => expr,
            // EManagedArrayType.TPBDRigidClusteredParticleHandle3fPtr => expr,
            // EManagedArrayType.FConvexUniquePtr => expr,
            EManagedArrayType.Vector2DArray => new TManagedArray<FVector2D[]>(Ar.ReadArray<FVector2D>),
            EManagedArrayType.Double => new TManagedArray<double>(Ar.Read<double>, false),
            EManagedArrayType.IntVector4 => new TManagedArray<TIntVector4<int>>(Ar.Read<TIntVector4<int>>, false),
            EManagedArrayType.Vector3d => new TManagedArray<FVector>(() => new FVector(Ar)),
            EManagedArrayType.IntVector2 => new TManagedArray<FIntVector2>(Ar.Read<FIntVector2>, false),
            EManagedArrayType.IntVector2Array => new TManagedArray<FIntVector2[]>(Ar.ReadArray<FIntVector2>),
            EManagedArrayType.Int32Array => new TManagedArray<int[]>(Ar.ReadArray<int>),
            EManagedArrayType.FloatArray => new TManagedArray<float[]>(Ar.ReadArray<float>),
            EManagedArrayType.Vector4f => new TManagedArray<FVector4>(Ar.Read<FVector4>),
            EManagedArrayType.FVectorArray => new TManagedArray<FVector[]>(Ar.ReadArray<FVector>),
            // EManagedArrayType.TPBDRigidParticle3fUniquePtr => expr,
            EManagedArrayType.FImplicitObjectRefCountedPtr => new TManagedArray<FImplicitObject?>(Ar.ReadPtr<FImplicitObject>),
            EManagedArrayType.FConvexRefCountedPtr => new TManagedArray<FConvex?>(Ar.ReadPtr<FConvex>),
            EManagedArrayType.Transform3f => new TManagedArray<FTransform>(Ar.Read<FTransform>),
            EManagedArrayType.IntVector3Array => new TManagedArray<TIntVector3<int>>(Ar.Read<TIntVector3<int>>),
            EManagedArrayType.Vector4fArray => new TManagedArray<FVector4[]>(Ar.ReadArray<FVector4>),
            EManagedArrayType.PMatrix33d => new TManagedArray<FMatrix>(() => new FMatrix(Ar)),
            EManagedArrayType.PMatrix33dArray => new TManagedArray<FMatrix[]>(() => Ar.ReadArray(() => new FMatrix(Ar))),
            EManagedArrayType.FVector3fNestedArray => new TManagedArray<FVector[][]>(() => Ar.ReadArray(Ar.ReadArray<FVector>)),
            EManagedArrayType.UintVector2 => new TManagedArray<TIntVector2<uint>>(Ar.Read<TIntVector2<uint>>, false),
            EManagedArrayType.UObjectArray => new TManagedArray<FPackageIndex>(() => new FPackageIndex(Ar)),
            // EManagedArrayType.LinearCurve => expr,
            EManagedArrayType.Name => new TManagedArray<FName>(Ar.ReadFName),
            EManagedArrayType.SoftObjectPath => new TManagedArray<FSoftObjectPath>(() => new FSoftObjectPath(Ar)),
            _ => throw new NotImplementedException($"EManagedArrayType Type: '{ArrayType}' currently does not have serialization implemented")
        };
    }
}

public enum EManagedArrayType : byte
{
    FNoneType,
    Vector,
    IntVector,
    Vector2D,
    LinearColor,
    Int32,
    Bool,
    Transform,
    String,
    Float,
    Quat,
    BoneNode,
    MeshSection,
    Box,
    IntArray,
    Guid,
    UInt8,
    VectorArrayPointer,
    VectorArrayUniquePointer,
    FImplicitObject3Pointer,
    FImplicitObject3UniquePointer,
    FImplicitObject3SerializablePtr,
    FBVHParticlesFloat3Pointer,
    FBVHParticlesFloat3UniquePointer,
    TPBDRigidParticleHandle3fPtr,
    TPBDGeometryCollectionParticleHandle3fPtr,
    TGeometryParticle3fUniquePtr,
    FImplicitObject3ThreadSafeSharedPointer,
    FImplicitObject3SharedPointer,
    TPBDRigidClusteredParticleHandle3fPtr,
    FConvexUniquePtr,
    Vector2DArray,
    Double,
    IntVector4,
    Vector3d,
    IntVector2,
    IntVector2Array,
    Int32Array,
    FloatArray,
    Vector4f,
    FVectorArray,
    TPBDRigidParticle3fUniquePtr,
    FImplicitObjectRefCountedPtr,
    FConvexRefCountedPtr,
    Transform3f,
    IntVector3Array,
    Vector4fArray,
    PMatrix33d,
    PMatrix33dArray,
    FVector3fNestedArray,
    UintVector2,
    UObjectArray,
    LinearCurve,
    Name,
    SoftObjectPath
}