using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

// CAkActorMixer
public class HierarchyActorMixer : BaseHierarchy
{
    public readonly uint[] ChildIds;

    // CAkActorMixer::SetInitialValues
    public HierarchyActorMixer(FArchive Ar) : base(Ar)
    {
        ChildIds = new AkChildren(Ar).ChildIds;
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(ChildIds));
        serializer.Serialize(writer, ChildIds);

        writer.WriteEndObject();
    }
}

