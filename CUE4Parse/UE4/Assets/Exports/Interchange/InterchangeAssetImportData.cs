using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Assets.Exports.Interchange;

public class AssetImportData : UObject
{
    public string CachedNodeContainer;
    public string CachedPipelines;
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        // it tries to read a FString but PropertyTag is FName???? 
        //base.Deserialize(Ar, validPos);
        CachedNodeContainer = Ar.ReadString();
        CachedPipelines = Ar.ReadString();
    }

    protected internal override void WriteJson(Newtonsoft.Json.JsonWriter writer, Newtonsoft.Json.JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        if (CachedNodeContainer is not null)
        {
            writer.WritePropertyName("CachedNodeContainer");
            serializer.Serialize(writer, CachedNodeContainer);
        }
        if (CachedPipelines is not null)
        {
            writer.WritePropertyName("CachedPipelines");
            serializer.Serialize(writer, CachedPipelines);
        }
    }
}
