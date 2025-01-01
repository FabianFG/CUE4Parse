using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject;

public class UModelStreamableData : UObject
{
    public bool bCooked;
    public FModelStreamableBulkData StreamingData;
    
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        bCooked = Ar.ReadBoolean();
        if (bCooked)
        {
            StreamingData = new FModelStreamableBulkData(Ar, bCooked);
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        
        writer.WritePropertyName("bCooked");
        serializer.Serialize(writer, bCooked);
        
        writer.WritePropertyName("StreamingData");
        serializer.Serialize(writer, StreamingData);
    }
}