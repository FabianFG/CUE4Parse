using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public class FKeyType
{
    public FName Name;
    public FName Group;

    public FKeyType(FAssetArchive Ar)
    {
        Name = Ar.ReadFName();
        Group = Ar.ReadFName();
    }
}
