using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

public class HierarchyEffect : AbstractHierarchy
{
    public HierarchyEffect(FWwiseArchive Ar) : base()
    {
        Id = Ar.Read<uint>();
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer) { }
}
