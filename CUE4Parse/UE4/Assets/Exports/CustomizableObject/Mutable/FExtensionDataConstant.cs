using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public class FExtensionDataConstant
{
    public FExtensionData Data;

    public FExtensionDataConstant(FArchive Ar)
    {
        Data = new FExtensionData(Ar);
    }
}
