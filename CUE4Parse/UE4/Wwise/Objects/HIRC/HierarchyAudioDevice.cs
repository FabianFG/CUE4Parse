using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC;

public class HierarchyAudioDevice : BaseHierarchyFx
{
    public readonly AkFXParams FXParams;

    public HierarchyAudioDevice(FArchive Ar) : base(Ar)
    {
        FXParams = new AkFXParams(Ar);
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WritePropertyName("FXParams");
        serializer.Serialize(writer, FXParams);

        writer.WriteEndObject();
    }
}
