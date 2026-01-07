using CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public class UCustomizableObject : UObject
{
    public long InternalVersion;
    public FModel? Model;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        
        InternalVersion = Ar.Read<long>();
        if (InternalVersion != -1)
            Model = new FModel(new FMutableArchive(Ar));
    }
}