using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public class FModel
{
    public FProgram Program;
    
    public FModel(FMutableArchive Ar)
    {
        Program = new FProgram(Ar);
    }
}