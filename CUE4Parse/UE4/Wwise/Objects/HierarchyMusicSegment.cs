using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects;

public class HierarchyMusicSegment : AbstractHierarchy
{
    public BaseHierarchyContainer ContainerHierarchy { get; private set; }

    public byte Flags { get; private set; }
    public uint[] ChildIDs { get; private set; }

    public HierarchyMusicSegment(FArchive Ar) : base(Ar)
    {
        if (WwiseVersions.WwiseVersion > 89)
        {
            Flags = Ar.Read<byte>();
        }
        else
        {
            Flags = 0;
        }

        // Step back so AbstractHierarchy starts reading correctly, since ID is read twice
        Ar.Position -= 4;

        ContainerHierarchy = new BaseHierarchyContainer(Ar);

        ChildIDs = new AkChildren(Ar).ChildIDs;
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Flags");
        writer.WriteValue(Flags);

        writer.WritePropertyName("ContainerHierarchy");
        writer.WriteStartObject();
        ContainerHierarchy.WriteJson(writer, serializer);
        writer.WriteEndObject();

        writer.WritePropertyName("ChildIDs");
        serializer.Serialize(writer, ChildIDs);

        writer.WriteEndObject();
    }
}
