using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Engine.EditorFramework;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CUE4Parse.UE4.Assets.Exports.Interchange;

public class UAssetImportData : UObject
{
    public FSourceFile[]? SourceData => _sourceData.Value;
    private Lazy<FSourceFile[]?> _sourceData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if (Ar.Ver >= EUnrealEngineObjectUE4Version.ASSET_IMPORT_DATA_AS_JSON && !Ar.IsFilterEditorOnly)
        {
            var json = Ar.ReadFString();
            _sourceData = new Lazy<FSourceFile[]?>(() => JsonConvert.DeserializeObject<FSourceFile[]?>(json));
        }

        base.Deserialize(Ar, validPos);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        
        writer.WritePropertyName("SourceData");
        serializer.Serialize(writer, SourceData);
    }
}