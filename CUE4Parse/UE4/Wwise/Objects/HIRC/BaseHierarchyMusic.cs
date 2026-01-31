using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Wwise.Enums.Flags;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class BaseHierarchyMusic : AbstractHierarchy
{
    public readonly BaseHierarchy ContainerHierarchy;
    public readonly EMusicFlags Flags;
    public readonly uint[] ChildIds;

    protected BaseHierarchyMusic(FArchive Ar) : base(Ar)
    {
        Flags = WwiseVersions.Version > 89 ? Ar.Read<EMusicFlags>() : EMusicFlags.None;
        Ar.Position -= 4; // Step back so AbstractHierarchy starts reading correctly, since ID is read twice
        ContainerHierarchy = new BaseHierarchy(Ar);
        ChildIds = new AkChildren(Ar).ChildIds;
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WritePropertyName(nameof(Flags));
        writer.WriteValue(Flags.ToString());

        writer.WritePropertyName(nameof(ContainerHierarchy));
        writer.WriteStartObject();
        ContainerHierarchy.WriteJson(writer, serializer);
        writer.WriteEndObject();

        writer.WritePropertyName(nameof(ChildIds));
        serializer.Serialize(writer, ChildIds);
    }
}
