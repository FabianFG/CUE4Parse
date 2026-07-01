using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchyGeneric : AbstractHierarchy
{
    public HierarchyGeneric(FWwiseArchive Ar) : base()
    {
        Id = Ar.Read<uint>();
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer) { }
}
