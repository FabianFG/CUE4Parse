using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public class FManagedArrayCollection
{
    public int Version;
    public Dictionary<FName, FGroupInfo> GroupInfo;
    public Dictionary<FMapKey, FValueType> Map;
    
    public FManagedArrayCollection(FChaosArchive Ar)
    {
        Version = Ar.Read<int>();
        GroupInfo = Ar.ReadMap(Ar.ReadFName, () => new FGroupInfo(Ar));
        Map = Ar.ReadMap(() => new FMapKey(Ar), () => new FValueType(Ar));
    }
}