using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Wwise.Objects.HIRC.Containers;

public class HierarchyAudioDevice : BaseHierarchyFx
{
    public readonly AkFxParams FxParams;

    // CAkAudioDevice::SetInitialValues
    public HierarchyAudioDevice(FArchive Ar) : base(Ar)
    {
        if (WwiseVersions.Version > 136)
        {
            FxParams = new AkFxParams(Ar); // AkOwnedEffectSlots::SetInitialValues
        }
    }

    public override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(FxParams));
        serializer.Serialize(writer, FxParams);

        writer.WriteEndObject();
    }
}
