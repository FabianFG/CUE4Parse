using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public class FExtensionDataConstant
{
    public ExtensionData Data;

    public FExtensionDataConstant(FArchive Ar)
    {
        Data = new ExtensionData(Ar);
    }
}