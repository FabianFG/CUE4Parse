using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public class FModel
{
    public uint Version;
    public FProgram Program;

    public FModel(FArchive Ar)
    {
        Version = Ar.Read<uint>();
        Program = new FProgram(Ar);
    }
}
