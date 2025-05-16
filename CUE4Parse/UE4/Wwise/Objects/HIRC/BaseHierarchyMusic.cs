using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public abstract class BaseHierarchyMusic : AbstractHierarchy
{
    public BaseHierarchy ContainerHierarchy { get; private set; }

    public EMusicFlags Flags { get; private set; }
    public uint[] ChildIds { get; private set; }

    protected BaseHierarchyMusic(FArchive Ar) : base(Ar)
    {
        Flags = WwiseVersions.WwiseVersion > 89 ? Ar.Read<EMusicFlags>() : EMusicFlags.None;
        Ar.Position -= 4; // Step back so AbstractHierarchy starts reading correctly, since ID is read twice
        ContainerHierarchy = new BaseHierarchy(Ar);
        ChildIds = new AkChildren(Ar).ChildIds;
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WritePropertyName("Flags");
        writer.WriteValue(Flags.ToString());

        writer.WritePropertyName("ContainerHierarchy");
        writer.WriteStartObject();
        ContainerHierarchy.WriteJson(writer, serializer);
        writer.WriteEndObject();

        writer.WritePropertyName("ChildIds");
        serializer.Serialize(writer, ChildIds);
    }
}
