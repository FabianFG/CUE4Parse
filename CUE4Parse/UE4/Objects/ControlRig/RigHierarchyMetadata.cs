using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;

namespace CUE4Parse.UE4.Objects.ControlRig;

public enum ERigMetadataType : byte
{
    Bool,
    BoolArray,
    Float,
    FloatArray,
    Int32,
    Int32Array,
    Name,
    NameArray,
    Vector,
    VectorArray,
    Rotator,
    RotatorArray,
    Quat,
    QuatArray,
    Transform,
    TransformArray,
    LinearColor,
    LinearColorArray,
    RigElementKey,
    RigElementKeyArray,

    /** MAX - invalid */
    Invalid,
}

public abstract class FRigBaseMetadata
{
    public FName Name;
    public ERigMetadataType Type = ERigMetadataType.Invalid;

    public bool IsValid => Type != ERigMetadataType.Invalid;
}

public class FRigBaseMetadata<T> : FRigBaseMetadata
{
    public T Value;

    public static FRigBaseMetadata Read(FAssetArchive Ar, bool isStorage = true)
    {
        var name = Ar.ReadFName();
        var type = isStorage ? Ar.Read<ERigMetadataType>() : EnumUtils.GetValueByName<ERigMetadataType>(Ar.ReadFName().Text);

        FRigBaseMetadata metadata = type switch
        {
            ERigMetadataType.Bool => new FRigBoolMetadata { Value = Ar.ReadBoolean() },
            ERigMetadataType.BoolArray => new FRigBoolArrayMetadata { Value = Ar.ReadArray(Ar.ReadBoolean) },
            ERigMetadataType.Float => new FRigFloatMetadata { Value = Ar.Read<float>() },
            ERigMetadataType.FloatArray => new FRigFloatArrayMetadata { Value = Ar.ReadArray<float>() },
            ERigMetadataType.Int32 => new FRigInt32Metadata { Value = Ar.Read<int>() },
            ERigMetadataType.Int32Array => new FRigInt32ArrayMetadata { Value = Ar.ReadArray<int>() },
            ERigMetadataType.Name => new FRigNameMetadata { Value = Ar.ReadFName() },
            ERigMetadataType.NameArray => new FRigNameArrayMetadata { Value = Ar.ReadArray(Ar.ReadFName) },
            ERigMetadataType.Vector => new FRigVectorMetadata { Value = new FVector(Ar) },
            ERigMetadataType.VectorArray => new FRigVectorArrayMetadata { Value = Ar.ReadArray(() => new FVector(Ar)) },
            ERigMetadataType.Rotator => new FRigRotatorMetadata{ Value = new FRotator(Ar) },
            ERigMetadataType.RotatorArray => new FRigRotatorArrayMetadata { Value = Ar.ReadArray(() => new FRotator(Ar)) },
            ERigMetadataType.Quat => new FRigQuatMetadata { Value = new FQuat(Ar) },
            ERigMetadataType.QuatArray => new FRigQuatArrayMetadata { Value = Ar.ReadArray(() => new FQuat(Ar)) },
            ERigMetadataType.Transform => new FRigTransformMetadata { Value = new FTransform(Ar) },
            ERigMetadataType.TransformArray => new FRigTransformArrayMetadata { Value = Ar.ReadArray(() => new FTransform(Ar)) },
            ERigMetadataType.LinearColor => new FRigLinearColorMetadata { Value = Ar.Read<FLinearColor>() },
            ERigMetadataType.LinearColorArray => new FRigLinearColorArrayMetadata { Value = Ar.ReadArray<FLinearColor>() },
            ERigMetadataType.RigElementKey => new FRigRigElementKeyMetadata { Value = new FRigElementKey(Ar) },
            ERigMetadataType.RigElementKeyArray => new FRigRigElementKeyArrayMetadata { Value = Ar.ReadArray(() => new FRigElementKey(Ar)) },
            _ => throw new ParserException($"Unknown ERigMetadataType value {type}")
        };
        metadata.Name = name;
        metadata.Type = type;
        return metadata;
    }
}

public class FRigBoolMetadata : FRigBaseMetadata<bool>;
public class FRigBoolArrayMetadata : FRigBaseMetadata<bool[]>;
public class FRigFloatMetadata : FRigBaseMetadata<float>;
public class FRigFloatArrayMetadata : FRigBaseMetadata<float[]>;
public class FRigInt32Metadata : FRigBaseMetadata<int>;
public class FRigInt32ArrayMetadata : FRigBaseMetadata<int[]>;
public class FRigNameMetadata : FRigBaseMetadata<FName>;
public class FRigNameArrayMetadata : FRigBaseMetadata<FName[]>;
public class FRigVectorMetadata : FRigBaseMetadata<FVector>;
public class FRigVectorArrayMetadata : FRigBaseMetadata<FVector[]>;
public class FRigRotatorMetadata : FRigBaseMetadata<FRotator>;
public class FRigRotatorArrayMetadata : FRigBaseMetadata<FRotator[]>;
public class FRigQuatMetadata : FRigBaseMetadata<FQuat>;
public class FRigQuatArrayMetadata : FRigBaseMetadata<FQuat[]>;
public class FRigTransformMetadata : FRigBaseMetadata<FTransform>;
public class FRigTransformArrayMetadata : FRigBaseMetadata<FTransform[]>;
public class FRigLinearColorMetadata : FRigBaseMetadata<FLinearColor>;
public class FRigLinearColorArrayMetadata : FRigBaseMetadata<FLinearColor[]>;
public class FRigRigElementKeyMetadata : FRigBaseMetadata<FRigElementKey>;
public class FRigRigElementKeyArrayMetadata : FRigBaseMetadata<FRigElementKey[]>;

public struct FMetadataStorage
{
    public Dictionary<FName, FRigBaseMetadata> MetadataMap;

    public FMetadataStorage(FAssetArchive Ar)
    {
        var num = Ar.Read<int>();
        MetadataMap = new Dictionary<FName, FRigBaseMetadata>(num);
        for (var i = 0; i < num; i++)
        {
            FName MetadataName = Ar.ReadFName();
            FName MetadataTypeName = Ar.ReadFName();
            MetadataMap[MetadataName] = FRigBoolMetadata.Read(Ar);
        }
    }
}
