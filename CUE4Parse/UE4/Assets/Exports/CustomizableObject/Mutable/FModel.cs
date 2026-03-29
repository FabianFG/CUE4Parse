using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public class FModel(FMutableArchive Ar)
{
    public FProgram Program = new(Ar);
}