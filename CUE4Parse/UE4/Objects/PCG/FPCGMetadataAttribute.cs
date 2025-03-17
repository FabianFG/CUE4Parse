using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.PCG;

public enum EPCGMetadataTypes : byte
{
    Float = 0,
    Double,
    Integer32,
    Integer64,
    Vector2,
    Vector,
    Vector4,
    Quaternion,
    Transform,
    String,
    Boolean,
    Rotator,
    Name,
    SoftObjectPath,
    SoftClassPath,

    Count,

    Unknown = 255,
};

public abstract class FPCGMetadataAttributeBase
{
    [JsonIgnore]
    public Dictionary<long, int> EntryToValueKeyMap;
    public int ParentAttributeId;
    public FName Name;
    public int AttributeId;

    public FPCGMetadataAttributeBase(FAssetArchive Ar)
    {
        EntryToValueKeyMap = Ar.ReadMap(Ar.Read<long>, Ar.Read<int>);
        ParentAttributeId = Ar.Read<int>();
        Name = Ar.ReadFName();
        AttributeId = Ar.Read<int>();
    }

    public static FPCGMetadataAttributeBase ReadPCGMetadataAttribute(FAssetArchive Ar)
    {
        var attributeTypeId = Ar.Read<int>();
        FPCGMetadataAttributeBase attribute = attributeTypeId switch
        {
            0 => new FPCGMetadataAttribute<float>(Ar, Ar.Read<float>),
            1 => new FPCGMetadataAttribute<double>(Ar, Ar.Read<double>),
            2 => new FPCGMetadataAttribute<int>(Ar, Ar.Read<int>),
            3 => new FPCGMetadataAttribute<long>(Ar, Ar.Read<long>),
            4 => new FPCGMetadataAttribute<FVector2D>(Ar, () => new FVector2D(Ar)),
            5 => new FPCGMetadataAttribute<FVector>(Ar, () => new FVector(Ar)),
            6 => new FPCGMetadataAttribute<FVector4>(Ar, () => new FVector4(Ar)),
            7 => new FPCGMetadataAttribute<FQuat>(Ar, () => new FQuat(Ar)),
            8 => new FPCGMetadataAttribute<FTransform>(Ar, () => new FTransform(Ar)),
            9 => new FPCGMetadataAttribute<string>(Ar, Ar.ReadFString),
            10 => new FPCGMetadataAttribute<bool>(Ar, Ar.ReadFlag),
            11 => new FPCGMetadataAttribute<FRotator>(Ar, () => new FRotator(Ar)),
            12 => new FPCGMetadataAttribute<FName>(Ar, Ar.ReadFName),
            13 or 14 => new FPCGMetadataAttribute<FSoftObjectPath>(Ar, () => new FSoftObjectPath(Ar)),
            _ => throw new ParserException($"Unknown EPCGMetadataTypes value {attributeTypeId}")
        };
        if (attributeTypeId == 10)
            Ar.Position += 3;
        return attribute;
    }
}

public class FPCGMetadataAttribute<T> : FPCGMetadataAttributeBase
{
    [JsonIgnore]
    public T[] Values = [];
    public T DefaultValue = default;

    public FPCGMetadataAttribute(FAssetArchive Ar, Func<T> getter) : base(Ar)
    {
        Values = Ar.ReadArray(getter);
        DefaultValue = getter();
    }
}
