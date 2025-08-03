using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;
using System.Collections.Generic;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json.Linq;

namespace CUE4Parse.UE4.Assets.Exports.Interchange;

public class UInterchangeAssetImportData : UAssetImportData
{
    public byte[] CachedNodeContainer = [];
    public List<KeyValuePair<string, JToken>> CachedPipelines = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (FInterchangeCustomVersion.Get(Ar) >= FInterchangeCustomVersion.Type.SerializedInterchangeObjectStoring)
        {
            long count = Ar.Read<long>();
            CachedNodeContainer = Ar.ReadBytes((int)count);

            int numPipelines = Ar.Read<int>();
            for (int i = 0; i < numPipelines; i++)
            {
                var key = Ar.ReadFString();
                var value = Ar.ReadFString();
                JToken jsonObj = JToken.Parse(value);
                CachedPipelines.Add(new KeyValuePair<string, JToken>(key, jsonObj));
            }
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        
        writer.WritePropertyName("CachedNodeContainer");
        writer.WriteValue(CachedNodeContainer);

        writer.WritePropertyName("CachedPipelines");
        serializer.Serialize(writer, CachedPipelines);
    }
}