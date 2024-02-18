using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

// Handler for UCustomizableObject .mut files
public class Model
{
    public int Version;
    public FProgram Program;

    public Model(FArchive Ar)
    {
        Version = Ar.Read<int>();
        Program = new FProgram(Ar);
    }
}
