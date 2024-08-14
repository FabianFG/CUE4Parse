using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component.Atmosphere;

public class USkyAtmosphereComponent : USceneComponent
{
    public FGuid bStaticLightingBuiltGUID;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        bStaticLightingBuiltGUID = Ar.Read<FGuid>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("bStaticLightingBuiltGUID");
        writer.WriteValue(bStaticLightingBuiltGUID.ToString(EGuidFormats.UniqueObjectGuid));
    }
}
