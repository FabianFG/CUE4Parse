using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Parameters;

public class FIntValueDesc
{
    public short Value;
    public string Name;

    public FIntValueDesc(FArchive Ar)
    {
        Value = Ar.Read<short>();
        Name = Ar.ReadMutableFString();
    }
}