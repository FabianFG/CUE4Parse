using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

// CAkBankMgr::StdBankRead<CAkDialogueEvent>
public class HierarchyDialogueEvent(FWwiseArchive Ar) : AbstractHierarchy(Ar)
{
    public override void WriteJson(JsonWriter writer, JsonSerializer serializer) { }
}
