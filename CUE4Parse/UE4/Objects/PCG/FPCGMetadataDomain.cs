using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.PCG;

public class FPCGMetadataDomain
{
    public Dictionary<FName, FPCGMetadataAttributeBase> Attributes = [];
    public long[] ParentKeys = [];

    public FPCGMetadataDomain(FAssetArchive Ar)
    {
        Attributes = Ar.ReadMap(Ar.ReadFName, () => FPCGMetadataAttributeBase.ReadPCGMetadataAttribute(Ar));
        ParentKeys = Ar.ReadArray<long>();
    }
}
