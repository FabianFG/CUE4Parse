using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchyAudioDevice : BaseHierarchyFx
{
    public readonly AkFxParams FxParams;

    public HierarchyAudioDevice(FArchive Ar) : base(Ar)
    {
        FxParams = new AkFxParams(Ar);
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WritePropertyName("FxParams");
        serializer.Serialize(writer, FxParams);

        writer.WriteEndObject();
    }
}
