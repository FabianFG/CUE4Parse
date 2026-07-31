using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public readonly struct FValueType
{
    public readonly EManagedArrayType ArrayType;
    public readonly FName? GroupIndexDependency;
    public readonly bool bPersistent;
    public readonly FManagedArrayBase ManagedArray;

    public FValueType(FChaosArchive Ar)
    {
        var serializationVersion = Ar.Read<int>(); // 3 in 4.23
        if (serializationVersion > 4) throw new ParserException(Ar, $"FValueType Serialization Version ({serializationVersion}) > 4");

        var arrayTypeAsInt = Ar.Read<int>();
        ArrayType = (EManagedArrayType) arrayTypeAsInt;

        if (Ar.Game == GAME_MarvelRivals && ArrayType == EManagedArrayType.FImplicitObjectRefCountedPtr)
            ArrayType = EManagedArrayType.Transform3f;

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
            ManagedArray.Serialize(Ar, ArrayType is
                EManagedArrayType.Vector or
                EManagedArrayType.IntVector or
                EManagedArrayType.Vector2D or
                EManagedArrayType.Int32 or
                EManagedArrayType.Bool or
                EManagedArrayType.Float or
                EManagedArrayType.Guid or
                EManagedArrayType.UInt8 or
                EManagedArrayType.Double or
                EManagedArrayType.IntVector4 or
                EManagedArrayType.IntVector2 or
                EManagedArrayType.UintVector2
            );
        }
    }

    private FManagedArrayBase CreateManagedArray(FChaosArchive Ar)
    {
        return ArrayType switch
        {
            EManagedArrayType.None => new FManagedArray<object>(() => null!),
            EManagedArrayType.Vector => new FManagedArray<FVector>(Ar.Read<FVector>),
            EManagedArrayType.IntVector => new FManagedArray<FIntVector>(Ar.Read<FIntVector>),
            EManagedArrayType.Vector2D => new FManagedArray<FVector2D>(Ar.Read<FVector2D>),
            EManagedArrayType.LinearColor => new FManagedArray<FLinearColor>(Ar.Read<FLinearColor>),
            EManagedArrayType.Int32 => new FManagedArray<int>(Ar.Read<int>),
            EManagedArrayType.Bool => new FManagedArray<bool>(Ar.ReadFlag),
            EManagedArrayType.Transform => new FManagedArray<FTransform>(() => new FTransform(Ar)),
            EManagedArrayType.String => new FManagedArray<string>(Ar.ReadFString),
            EManagedArrayType.Float => new FManagedArray<float>(Ar.Read<float>),
            EManagedArrayType.Quat => new FManagedArray<FQuat>(() => new FQuat(Ar)),
            EManagedArrayType.BoneNode => new FManagedArray<FGeometryCollectionBoneNode>(() => new FGeometryCollectionBoneNode(Ar)),
            // UE 4.22
            EManagedArrayType.MeshSection => new FManagedArray<FGeometryCollectionSection>(Ar.Read<FGeometryCollectionSection>),
            EManagedArrayType.Box => new FManagedArray<FBox>(() => new FBox(Ar)),
            EManagedArrayType.IntArray => new FManagedArray<int[]>(Ar.ReadArray<int>), // This should be a TSet/HashSet
            // UE 4.23
            EManagedArrayType.Guid => new FManagedArray<FGuid>(Ar.Read<FGuid>),
            EManagedArrayType.UInt8 => new FManagedArray<byte>(Ar.Read<byte>),
            // EManagedArrayType.VectorArrayPointer => expr,
            // EManagedArrayType.VectorArrayUniquePointer => expr,
            EManagedArrayType.FImplicitObject3Pointer => new FManagedArray<FImplicitObject?>(Ar.ReadPtr<FImplicitObject>),
            EManagedArrayType.FImplicitObject3UniquePointer => new FManagedArray<FImplicitObject?>(Ar.ReadPtr<FImplicitObject>),
            EManagedArrayType.FImplicitObject3SerializablePtr => new FManagedArray<FImplicitObject?>(Ar.ReadPtr<FImplicitObject>),
            EManagedArrayType.FBVHParticlesFloat3Pointer => new FManagedArray<FBVHParticles?>(Ar.ReadPtr<FBVHParticles>),
            EManagedArrayType.FBVHParticlesFloat3UniquePointer => new FManagedArray<FBVHParticles?>(Ar.ReadPtr<FBVHParticles>),
            // UE 4.24
            // EManagedArrayType.TPBDRigidParticleHandle3fPtr => expr,
            // EManagedArrayType.TPBDGeometryCollectionParticleHandle3fPtr => expr,
            // EManagedArrayType.TGeometryParticle3fUniquePtr => expr,
            // UE 4.25
            EManagedArrayType.FImplicitObject3ThreadSafeSharedPointer => new FManagedArray<FImplicitObject?>(Ar.ReadPtr<FImplicitObject>),
            EManagedArrayType.FImplicitObject3SharedPointer => new FManagedArray<FImplicitObject?>(Ar.ReadPtr<FImplicitObject>),
            // EManagedArrayType.TPBDRigidClusteredParticleHandle3fPtr => expr,
            // UE 5.0
            EManagedArrayType.FConvexUniquePtr => new FManagedArray<FConvex?>(Ar.ReadPtr<FConvex>),//or implicitObject
            EManagedArrayType.Vector2DArray => new FManagedArray<FVector2D[]>(Ar.ReadArray<FVector2D>),
            EManagedArrayType.Double => new FManagedArray<double>(Ar.Read<double>),
            // UE 5.1
            EManagedArrayType.IntVector4 => new FManagedArray<TIntVector4<int>>(Ar.Read<TIntVector4<int>>),
            EManagedArrayType.Vector3d => new FManagedArray<FVector>(() => new FVector(Ar)),
            EManagedArrayType.IntVector2 => new FManagedArray<FIntVector2>(Ar.Read<FIntVector2>),
            EManagedArrayType.IntVector2Array => new FManagedArray<FIntVector2[]>(Ar.ReadArray<FIntVector2>),
            EManagedArrayType.Int32Array => new FManagedArray<int[]>(Ar.ReadArray<int>),
            EManagedArrayType.FloatArray => new FManagedArray<float[]>(Ar.ReadArray<float>),
            EManagedArrayType.Vector4f => new FManagedArray<FVector4>(Ar.Read<FVector4>),
            EManagedArrayType.FVectorArray => new FManagedArray<FVector[]>(Ar.ReadArray<FVector>),
            // UE 5.2
            // EManagedArrayType.TPBDRigidParticle3fUniquePtr => expr,
            // UE 5.4
            EManagedArrayType.FImplicitObjectRefCountedPtr => new FManagedArray<FImplicitObject?>(Ar.ReadPtr<FImplicitObject>),
            EManagedArrayType.FConvexRefCountedPtr => new FManagedArray<FConvex?>(Ar.ReadPtr<FConvex>),
            EManagedArrayType.Transform3f => new FManagedArray<FTransform>(Ar.Read<FTransform>),
            EManagedArrayType.IntVector3Array => new FManagedArray<TIntVector3<int>>(Ar.Read<TIntVector3<int>>),
            EManagedArrayType.Vector4fArray => new FManagedArray<FVector4[]>(Ar.ReadArray<FVector4>),
            // UE 5.5
            EManagedArrayType.PMatrix33d => new FManagedArray<FMatrix>(() => new FMatrix(Ar)),
            EManagedArrayType.PMatrix33dArray => new FManagedArray<FMatrix[]>(() => Ar.ReadArray(() => new FMatrix(Ar))),
            EManagedArrayType.FVector3fNestedArray => new FManagedArray<FVector[][]>(() => Ar.ReadArray(Ar.ReadArray<FVector>)),
            // UE 5.6
            EManagedArrayType.UintVector2 => new FManagedArray<TIntVector2<uint>>(Ar.Read<TIntVector2<uint>>),
            EManagedArrayType.UObjectArray => new FManagedArray<FPackageIndex>(() => new FPackageIndex(Ar)),
            // EManagedArrayType.LinearCurve => expr,
            // UE 5.7
            EManagedArrayType.Name => new FManagedArray<FName>(Ar.ReadFName),
            EManagedArrayType.SoftObjectPath => new FManagedArray<FSoftObjectPath>(() => new FSoftObjectPath(Ar)),
            _ => throw new NotImplementedException($"EManagedArrayType Type: '{ArrayType}' currently does not have serialization implemented")
        };
    }
    public override string ToString() => $"{ArrayType}[{ManagedArray?.Data?.Length ?? 0}]";
}
