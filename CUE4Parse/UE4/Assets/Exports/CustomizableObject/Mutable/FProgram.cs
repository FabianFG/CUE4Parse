using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable;

public class FProgram
{
    public string Name;
    // public 
    
    public FProgram(FArchive Ar)
    {
        Name = Ar.ReadFString();

        var type = Ar.Read<ushort>();
        var unusedFlags = Ar.Read<ushort>();

        var IntConstantArgs = Ar.Read<int>();
        var ScalarConstantArgs = Ar.Read<float>();
        var ColourConstantArgs = new float[4];
        for (var i = 0; i < ColourConstantArgs.Length; i++)
        {
            ColourConstantArgs[i] = Ar.Read<float>();
        }
    }
}