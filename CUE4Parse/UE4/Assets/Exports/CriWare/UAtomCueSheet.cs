using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.CriWare.Readers;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.CriWare;

public class UAtomCueSheet : UObject
{
    public AcbReader? AcbReader;
    public Dictionary<string, List<Dictionary<string, object?>>>? TableData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        Ar.Position += 2; // No clue

        var bulkData = new FByteBulkData(Ar);
        var savedPosition = Ar.Position;

        if (bulkData.Data == null)
            return;

        using var bulkAr = new FByteArchive("bulk", bulkData.Data);
        AcbReader = new AcbReader(bulkAr);
        TableData = AcbReader.TableData;

        if (bulkData.BulkDataFlags is EBulkDataFlags.BULKDATA_None)
        {
            Ar.Position = savedPosition + bulkData.Header.ElementCount;
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (TableData == null)
            return;

        writer.WritePropertyName(nameof(TableData));
        serializer.Serialize(writer, TableData);
    }
}
