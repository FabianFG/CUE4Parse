using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

// CAkActorMixer
public class HierarchyActorMixer : AbstractHierarchy
{
    public readonly BaseHierarchy BaseParams;
    public readonly uint[] ChildIds;

    // CAkActorMixer::SetInitialValues
    public HierarchyActorMixer(FWwiseArchive Ar) : base()
    {
        Id = Ar.Read<uint>();
        BaseParams = new BaseHierarchy(Ar);
        ChildIds = new AkChildren(Ar).ChildIds;
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(BaseParams));
        writer.WriteStartObject();
        BaseParams.WriteJson(writer, serializer);
        writer.WriteEndObject();

        writer.WritePropertyName(nameof(ChildIds));
        serializer.Serialize(writer, ChildIds);

        writer.WriteEndObject();
    }
}

