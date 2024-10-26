using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

// Handler for UCustomizableObject .mut files
public class Model
{
    public FProgram Program;

    public Model(FAssetArchive Ar)
    {
        Ar.Position += 4;
        Program = new FProgram(Ar);
    }
}
