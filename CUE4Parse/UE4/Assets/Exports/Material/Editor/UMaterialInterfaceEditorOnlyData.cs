using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Material.Editor;

public class UMaterialInterfaceEditorOnlyData : UObject
{
    public FStructFallback? CachedExpressionData;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        var bSavedCachedExpressionData = Ar.ReadBoolean();

        if (bSavedCachedExpressionData)
        {
            CachedExpressionData = new FStructFallback(Ar, "MaterialCachedExpressionEditorOnlyData");
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName("CachedExpressionData");
        serializer.Serialize(writer, CachedExpressionData);
    }
}