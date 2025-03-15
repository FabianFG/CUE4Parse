using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.PCG;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.PCG;

public class UPCGMetadata : UObject
{
    public Dictionary<FName, FPCGMetadataAttributeBase> Attributes;
    public long[] ParentKeys;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        Ar.Read<int>();
        base.Deserialize(Ar, validPos);

        var NumAttributes = Ar.Read<int>();
        Attributes = [];

        for (var i = 0; i < NumAttributes; i++)
        {
            var AttributeName = Ar.ReadFName();
            var AttributeTypeId = Ar.Read<int>();

            Attributes[AttributeName] = AttributeTypeId switch
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
                10 => new FPCGMetadataAttribute<bool>(Ar, Ar.ReadBoolean),
                11 => new FPCGMetadataAttribute<FRotator>(Ar, () => new FRotator(Ar)),
                12 => new FPCGMetadataAttribute<FName>(Ar, Ar.ReadFName),
                13 or 14 => new FPCGMetadataAttribute<FSoftObjectPath>(Ar, () => new FSoftObjectPath(Ar)),
                _ => throw new ParserException($"Unknown EPCGMetadataTypes value {AttributeTypeId}")
            };
        }

        ParentKeys = Ar.ReadArray<long>();
    }
}
