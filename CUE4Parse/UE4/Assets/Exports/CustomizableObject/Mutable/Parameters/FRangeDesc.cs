using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Parameters;

public class FRangeDesc
{
    public string Name;
    public string Uid;
    public int DimensionParameter;

    public FRangeDesc(FArchive Ar)
    {
        var version = Ar.Read<int>();

        Name = Ar.ReadMutableFString();
        Uid = Ar.ReadMutableFString();
        DimensionParameter = Ar.Read<int>();
    }
}
