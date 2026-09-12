using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.CriWare.Readers;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.CriWare;

public class UAtomCueSheet : UAtomSoundBank
{
    public AcbReader? AcbReader;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (RawData is null || RawData.ReadDataOnce() is not {Length: > 0} bulkData)
            return;

        using var bulkAr = new FByteArchive("AcbReader", bulkData);
        AcbReader = new AcbReader(bulkAr);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (AcbReader == null)
            return;

        writer.WritePropertyName(nameof(AcbReader.AtomCueSheetData));
        serializer.Serialize(writer, AcbReader.AtomCueSheetData);
    }
}
