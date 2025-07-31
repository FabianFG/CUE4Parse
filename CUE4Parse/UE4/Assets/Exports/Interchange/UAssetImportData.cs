using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CUE4Parse.UE4.Assets.Exports.Interchange;

public class UAssetImportData : UObject
{
    public JToken? SourceDataJson;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if (Ar.Ver >= EUnrealEngineObjectUE4Version.ASSET_IMPORT_DATA_AS_JSON && !Ar.IsFilterEditorOnly)
        {
            var json = Ar.ReadFString();
            SourceDataJson = JToken.Parse(json);
        }

        base.Deserialize(Ar, validPos);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        if (SourceDataJson != null)
        {
            writer.WritePropertyName("SourceData");
            SourceDataJson.WriteTo(writer);
        }
    }
}