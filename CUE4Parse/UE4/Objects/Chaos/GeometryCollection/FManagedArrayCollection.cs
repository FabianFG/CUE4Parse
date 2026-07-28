using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Objects.Chaos.GeometryCollection;

public class FManagedArrayCollection
{
    public int Version;
    public Dictionary<FName, FGroupInfo> GroupInfo;
    public Dictionary<FKeyType, FValueType> Map;

    public FManagedArrayCollection(FChaosArchive Ar)
    {
        Version = Ar.Read<int>();
        GroupInfo = Ar.ReadMap(Ar.ReadFName, () => new FGroupInfo(Ar));
        Map = Ar.ReadMap(() => new FKeyType(Ar), () => new FValueType(Ar));
    }

    public T[]? GetAttributeValue<T>(string attribute, string group) => GetAttributeValue<T>(attribute, new FName(group));
    public T[]? GetAttributeValue<T>(string attribute, FName group) => GetAttributeValue<T>(new FKeyType(attribute, group));
    public T[]? GetAttributeValue<T>(FKeyType key)
    {
        return Map.TryGetValue(key, out var value) ? value.ManagedArray.Data as T[] : null;
    }
}
