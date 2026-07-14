using System.Diagnostics;
using CUE4Parse.UE4.Assets.Exports.Chaos;
using CUE4Parse.UE4.Assets.Exports.Chaos.GeometryCollection;
using CUE4Parse.UE4.Objects.Chaos.GeometryCollection;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Chaos.GeometryCollection;

public class TManangedArray : FManagedArrayBase
{
    public EManagedArrayType ArrayType;

    public override void Serialize(FChaosArchive Ar)
    {
        var version = Ar.Read<int>(); // 1
        Debug.Assert(version == 1);

        // see TryBulkSerializeManagedArray in ManagedArray.h and fix this mess
        switch (ArrayType)
        {
            case EManagedArrayType.FTransformType:
                Serialize(Ar, () => new FTransform(Ar));
                break;
            case EManagedArrayType.Transform3fType:
                Serialize<FTransform>(Ar);
                break;
            case EManagedArrayType.FVector4fType:
                Serialize<FVector4>(Ar);
                break;
            case EManagedArrayType.FVectorType:
                SerializeAsBulk<FVector>(Ar);
                break;
            case EManagedArrayType.FVector2DType:
                SerializeAsBulk<FVector2D>(Ar);
                break;
            case EManagedArrayType.FIntVectorType:
                SerializeAsBulk<FIntVector>(Ar);
                break;
            case EManagedArrayType.FStringType:
                Data = Ar.ReadArray(() => Ar.ReadFString()) as object[];
                break;
            case EManagedArrayType.FLinearColorType:
                Serialize<FLinearColor>(Ar);
                break;
            case EManagedArrayType.FIntArrayType:
                // Int[][]
                // Data =
                // not serialized as bulk
                SerializeAsArray(Ar, Ar.ReadArray<int>); //here broken!
                break;
            case EManagedArrayType.FInt32Type:
                SerializeAsBulk<int>(Ar);

                break;
            case EManagedArrayType.FFloatType:
                SerializeAsBulk<float>(Ar);
                break;
            case EManagedArrayType.FBoolType:
                    SerializeAsBulk<bool>(Ar);
                break;
            case EManagedArrayType.FBoxType:
                if (Ar.Game == GAME_MarvelRivals)
                    Serialize<FBox>(Ar);
                else
                    Serialize(Ar, () => new FBox(Ar));
                break;
            case EManagedArrayType.FMeshSectionType:
                Serialize<FGeometryCollectionSection>(Ar);
                break;
            case EManagedArrayType.FFImplicitObjectRefCountedPtrType:
            case EManagedArrayType.FFImplicitObject3SharedPointerType:
            case EManagedArrayType.FFImplicitObject3ThreadSafeSharedPointerType:
            case EManagedArrayType.FFConvexUniquePtrType:
            case EManagedArrayType.FConvexRefCountedPtrType:
                SerializeBulkManagedPtrArray(Ar, () => new FImplicitObject());
                break;
            case EManagedArrayType.FFBVHParticlesFloat3UniquePointerType:
                SerializeBulkManagedPtrArray(Ar, () => new FBVHParticles());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ArrayType));
        }
    }

    // ManagedArray.h TManagedArrayBase::Serialize
    private void Serialize<T2>(FChaosArchive Ar) where T2 : struct
    {
        // var version = Ar.Read<int>(); // 1

        // if (FDestructionObjectVersion.Get(Ar) < FDestructionObjectVersion.Type.BulkSerializeArrays)
        // {
        var readArraydata = Ar.ReadArray<T2>();
        Data = Array.ConvertAll(readArraydata, x => (object)x);
        // }
        // else
        // {
            // Data = Ar.ReadBulkArray<T2>() as object[];
        // }
    }

    private void Serialize<T2>(FChaosArchive Ar, Func<T2> getter) where T2 : struct
    {
        // var version = Ar.Read<int>(); // 1

        // if (FDestructionObjectVersion.Get(Ar) < FDestructionObjectVersion.Type.BulkSerializeArrays)
        // {
        var readArraydata = Ar.ReadArray<T2>(getter);
        Data = Array.ConvertAll(readArraydata, x => (object)x);
        // }
        // else
        // {
        // Data = Ar.ReadBulkArray<T2>() as object[];
        // }
    }

    private void Serialize(FChaosArchive Ar, Func<object> getter)
    {
        // var version = Ar.Read<int>(); // 1

        // if (FDestructionObjectVersion.Get(Ar) < FDestructionObjectVersion.Type.BulkSerializeArrays)
        // {
        var readArraydata = Ar.ReadArray(getter);
        Data = Array.ConvertAll(readArraydata, x => (object)x);
        // }
        // else
        // {
        // Data = Ar.ReadBulkArray<T2>() as object[];
        // }
    }

    private void SerializeAsBulk<T2>(FChaosArchive Ar) where T2 : struct
    {
        var readArraydata = Ar.ReadBulkArray<T2>();
        Data = Array.ConvertAll(readArraydata, x => (object)x);
    }

    private void SerializeAsBulk(FChaosArchive Ar, Func<object> getter)
    {
        var readArraydata = Ar.ReadBulkArray(getter);
        Data = Array.ConvertAll(readArraydata, x => (object)x);
    }

    private void SerializeAsArray(FChaosArchive Ar, Func<object> getter)
    {
        // var version = Ar.Read<int>(); // 1
        // Debug.Assert(version == 1);
        // if (FDestructionObjectVersion.Get(Ar)< FDestructionObjectVersion.Type.BulkSerializeArrays)
        // {
            Data = Ar.ReadArray(getter) as object[];
        // }
        // else
        // {
        //     Data = Ar.ReadBulkArray(getter) as object[];
        // }
    }

    // private void SerializePtrArray(FChaosArchive Ar, Func<object> getter)
    // {
    //     // var version = Ar.Read<int>(); // 1
    //     // Debug.Assert(version == 1);
    //     if (FDestructionObjectVersion.Get(Ar) < FDestructionObjectVersion.Type.BulkSerializeArrays)
    //     {
    //         // Data = Ar.ReadArray(getter) as object[];
    //         //
    //         throw new NotImplementedException(nameof(SerializePtrArray));
    //     }
    //     else
    //     {
    //         Data = SerializeBulkManagedPtrArray(Ar, getter);
    //     }
    // }

    public void SerializeBulkManagedPtrArray<T2>(FChaosArchive Ar, Func<T2> getter) where T2 : ISerializationFactory
    {
        var count = Ar.Read<int>();

        var result = new T2[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = Ar.SerializePtr<T2>(getter());
        }

        Data = Array.ConvertAll(result, x => (object)x);
    }
}

public abstract class FManagedArrayBase
{
    [JsonIgnore]
    public object[] Data;

    public int DataLength => Data?.Length ?? 0;

    // FFVector3fType => Vector
    // FFIntVectorType => IntVector
    // FFVector2fType => Vector2D
    // FFLinearColorType => LinearColor
    // Fint32Type => Int32
    // FboolType => Bool
    // FFTransformType => Transform
    // FFStringType => String
    // FfloatType => Float
    // FFQuat4fType => Quat
    // FFGeometryCollectionBoneNodeType => BoneNode
    // FFGeometryCollectionSectionType => MeshSection
    // FFBoxType => Box
    // FTSetType<int32> => IntArray
    // FFGuidType => Guid
    // Fuint8Type => UInt8
    // FTArrayType<FVector3f>* => VectorArrayPointer
    // FTUniquePtrType<TArray<FVector3f>> => VectorArrayUniquePointer
    // FChaosType::FImplicitObject3* => FImplicitObject3Pointer
    // FTUniquePtrType<Chaos::FImplicitObject3> => FImplicitObject3UniquePointer
    // FChaosType::TSerializablePtr<Chaos::FImplicitObject3> => FImplicitObject3SerializablePtr
    // FChaosType::FBVHParticlesFloat3 => FBVHParticlesFloat3Pointer
    // FTUniquePtrType<Chaos::FBVHParticlesFloat3> => FBVHParticlesFloat3UniquePointer
    // FChaosType::FPBDRigidParticleHandle* => TPBDRigidParticleHandle3fPtr
    // FChaosType::FPBDGeometryCollectionParticleHandle* => TPBDGeometryCollectionParticleHandle3fPtr
    // FTUniquePtrType<Chaos::FGeometryParticle> => TGeometryParticle3fUniquePtr
    // FChaosType::ThreadSafeSharedPtr_FImplicitObject => FImplicitObject3ThreadSafeSharedPointer
    // FChaosType::NotThreadSafeSharedPtr_FImplicitObject => FImplicitObject3SharedPointer
    // FChaosType::FPBDRigidClusteredParticleHandle* => TPBDRigidClusteredParticleHandle3fPtr
    // FTUniquePtrType<Chaos::FConvex> => FConvexUniquePtr
    // FTArrayType<FVector2f> => Vector2DArray
    // FdoubleType => Double
    // FFIntVector4Type => IntVector4
    // FFVector3dType => Vector3d
    // FFIntVector2Type => IntVector2
    // FTArrayType<FIntVector2> => IntVector2Array
    // FTArrayType<int32> => Int32Array
    // FTArrayType<float> => FloatArray
    // FFVector4fType => Vector4f
    // FTArrayType<FVector3f> => FVectorArray
    // FTUniquePtrType<Chaos::FPBDRigidParticle> => TPBDRigidParticle3fUniquePtr
    public static FManagedArrayBase NewManagedTypedArray(EManagedArrayType arrayType)
    {
        return new TManangedArray { ArrayType = arrayType };

        // switch (arrayType)
        // {
        //     case EManagedArrayType.FVectorType:
        //         return new TManangedArray<FVector>();
        //     case EManagedArrayType.FIntVectorType:
        //         return new TManangedArray<FIntVector>();
        //     case EManagedArrayType.FVector2DType:
        //         return new TManangedArray<FVector2D>();
        //     case EManagedArrayType.FLinearColorType:
        //         return new TManangedArray<FLinearColor>();
        //     case EManagedArrayType.FInt32Type:
        //         return new TManangedArray<int>();
        //     case EManagedArrayType.FBoolType:
        //         return new TManangedArray<bool>();
        //     case EManagedArrayType.FTransformType:
        //         return new TManangedArray<FTransform>();
        //     case EManagedArrayType.FStringType:
        //         return new TManangedArray<string>();
        //     case EManagedArrayType.FFloatType:
        //         return new TManangedArray<float>();
        //     case EManagedArrayType.FQuatType:
        //         return new TManangedArray<FQuat>();
        //     case EManagedArrayType.FBoneNodeType:
        //         return new TManangedArray<FGeometryCollectionBoneNode>();
        //     case EManagedArrayType.FMeshSectionType:
        //         return new TManangedArray<FGeometryCollectionSection>();
        //     case EManagedArrayType.FBoxType:
        //         return new TManangedArray<FBox>();
        //     case EManagedArrayType.FGuidType:
        //         return new TManangedArray<FGuid>();
        //     case EManagedArrayType.FUInt8Type:
        //         return new TManangedArray<byte>();
        //     case EManagedArrayType.FFImplicitObjectRefCountedPtrType:
        //         return new TManangedArray<FImplicitObject>();
        //     case EManagedArrayType.FVectorArrayPointerType:
        //         return new TManangedArray<FVector>(); // TArray<FVector3f>* => VectorArrayPointer
        //     case EManagedArrayType.FVectorArrayUniquePointerType:
        //         return new TManangedArray<FVector>(); // TUniquePtr<TArray<FVector3f>> => VectorArrayUniquePointer
        // }

        throw new NotImplementedException(nameof(arrayType));
    }

    public abstract void Serialize(FChaosArchive Ar);
}
