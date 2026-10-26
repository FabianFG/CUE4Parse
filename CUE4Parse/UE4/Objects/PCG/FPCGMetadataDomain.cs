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
            // UE keys attributes by name, so a repeated name replaces the earlier one
            Attributes[attributeName] = FPCGMetadataAttributeBase.ReadPCGMetadataAttribute(Ar, attributeName);
        }

        ParentKeys = Ar.ReadArray<long>();
    }
}
