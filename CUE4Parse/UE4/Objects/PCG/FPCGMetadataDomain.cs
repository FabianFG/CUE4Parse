using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.PCG;

public class FPCGMetadataDomain
{
    public Dictionary<FName, FPCGMetadataAttributeBase> Attributes = [];
    public long[] ParentKeys = [];

    public FPCGMetadataDomain(FAssetArchive Ar)
    {
        var attributeCount = Ar.Read<int>();
        Attributes = new Dictionary<FName, FPCGMetadataAttributeBase>(attributeCount);
        for (var i = 0; i < attributeCount; i++)
        {
            var attributeName = Ar.ReadFName();
            Attributes.Add(attributeName, FPCGMetadataAttributeBase.ReadPCGMetadataAttribute(Ar, attributeName));
        }

        ParentKeys = Ar.ReadArray<long>();
    }
}
