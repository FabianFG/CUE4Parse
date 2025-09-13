using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UWwiseAssetLibrary : UObject
{
    public FWwiseAssetLibraryCookedData? CookedData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        CookedData = new FWwiseAssetLibraryCookedData(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(CookedData));
        writer.WriteStartObject();

        writer.WritePropertyName("PackagedFiles");
        serializer.Serialize(writer, CookedData?.PackagedFiles);

        writer.WriteEndObject();
    }
}
