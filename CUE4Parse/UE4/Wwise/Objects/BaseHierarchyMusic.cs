using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects;

public abstract class BaseHierarchyMusic : AbstractHierarchy
{
    public BaseHierarchy ContainerHierarchy { get; private set; }

    public byte Flags { get; private set; }
    public uint[] ChildIDs { get; private set; }

    protected BaseHierarchyMusic(FArchive Ar) : base(Ar)
    {
        Flags = WwiseVersions.WwiseVersion > 89 ? Ar.Read<byte>() : (byte) 0;
        Ar.Position -= 4; // Step back so AbstractHierarchy starts reading correctly, since ID is read twice
        ContainerHierarchy = new BaseHierarchy(Ar);
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
