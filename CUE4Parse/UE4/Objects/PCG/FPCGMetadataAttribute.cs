using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using System.Collections.Generic;
using System;

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
}

public class FPCGMetadataAttribute<T> : FPCGMetadataAttributeBase
{
    public T[] Values = [];
    public T DefaultValue = default;

    public FPCGMetadataAttribute(FAssetArchive Ar, Func<T> getter) : base(Ar)
    {
        Values = Ar.ReadArray(getter);
        DefaultValue = getter();
    }
}
