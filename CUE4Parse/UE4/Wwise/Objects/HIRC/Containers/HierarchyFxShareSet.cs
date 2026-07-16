using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

// CAkBankMgr::StdBankRead<CAkFxShareSet>
public class HierarchyFxShareSet(FWwiseArchive Ar) : BaseHierarchyFx(Ar)
{
    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WriteEndObject();
    }
}
