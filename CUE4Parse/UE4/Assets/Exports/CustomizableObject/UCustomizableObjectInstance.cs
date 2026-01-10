using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public class UCustomizableObjectInstance : UObject
{
    public FCustomizableObjectInstanceDescriptor? Descriptor;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Descriptor = GetOrDefault<FCustomizableObjectInstanceDescriptor>(nameof(Descriptor));
    }
}
