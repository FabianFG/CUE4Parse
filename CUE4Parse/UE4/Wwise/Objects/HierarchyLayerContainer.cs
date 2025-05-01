using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects;

public class HierarchyLayerContainer : BaseHierarchyContainer
{
    public uint[] ChildIDs { get; private set; }

    public HierarchyLayerContainer(FArchive Ar) : base(Ar)
    {
        ChildIDs = new CAkChildren(Ar).ChildIDs;
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WritePropertyName("ChildIDs");
        serializer.Serialize(writer, ChildIDs);

        writer.WriteEndObject();
    }
}
