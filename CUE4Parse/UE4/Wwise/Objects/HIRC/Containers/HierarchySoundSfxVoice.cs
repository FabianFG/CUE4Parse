using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

public class HierarchySoundSfxVoice : AbstractHierarchy
{
    public readonly AkBankSourceData Source;
    public readonly BaseHierarchy BaseParams;

    // CAkSound::SetInitialValues
    public HierarchySoundSfxVoice(FArchive Ar) : base(Ar)
    {
        Source = new AkBankSourceData(Ar);
        Ar.Position -= 4; // Step back so AbstractHierarchy starts reading correctly, since ID is read twice
        BaseParams = new BaseHierarchy(Ar);
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(nameof(Source));
        Source.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(BaseParams));
        writer.WriteStartObject();
        BaseParams.WriteJson(writer, serializer);
        writer.WriteEndObject();

        writer.WriteEndObject();
    }
}
