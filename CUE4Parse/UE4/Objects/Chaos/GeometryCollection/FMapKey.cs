using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public class FMapKey
{
    public FName Name;
    public FName Group;
    
    public FMapKey(FChaosArchive Ar)
    {
        Name = Ar.ReadFName();
        Group = Ar.ReadFName();
    }
}