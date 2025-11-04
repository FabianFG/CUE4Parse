using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.CriWare.Readers;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.CriWare;

public class USoundAtomConfig : UObject
{
    public Dictionary<string, List<Dictionary<string, object?>>>? TableData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        var bulkData = new FByteBulkData(Ar);
        var savedPosition = Ar.Position;

        if (bulkData.Data == null)
            return;

        using var bulkAr = new FByteArchive("bulk", bulkData.Data);
        using var acbReader = new AcbReader(bulkAr);

        TableData = acbReader.TableData;

        if (bulkData.BulkDataFlags is EBulkDataFlags.BULKDATA_None)
        {
            Ar.Position = savedPosition + bulkData.Header.ElementCount;
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(TableData));
        serializer.Serialize(writer, TableData);
    }
}
