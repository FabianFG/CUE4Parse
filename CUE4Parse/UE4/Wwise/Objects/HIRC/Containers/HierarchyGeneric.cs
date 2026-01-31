using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

public class HierarchyGeneric(FArchive Ar) : AbstractHierarchy(Ar)
{
    public override void WriteJson(JsonWriter writer, JsonSerializer serializer) { }
}
