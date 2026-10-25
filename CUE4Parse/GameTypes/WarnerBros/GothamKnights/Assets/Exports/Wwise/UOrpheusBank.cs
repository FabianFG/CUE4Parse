using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Wwise;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.WarnerBros.GothamKnights.Assets.Exports.Wwise;

public class UOrpheusBank : UObject
{
    public uint SoundBankId;
    public WwiseReader? SoundBank;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        SoundBankId = Ar.Read<uint>();
        Ar.Position += 8;
        var soundBankSize = Ar.Read<long>();
        if (Ar.Position + soundBankSize > Ar.Length)
            throw new ParserException($"Soundbank with size {soundBankSize} is larger than remaining archive length");

        using var reader = new FWwiseArchive(Ar);
        SoundBank = new WwiseReader(reader, new WwiseArchiveSource());
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(SoundBankId));
        writer.WriteValue(SoundBankId);

        if (SoundBank is not null)
        {
            writer.WritePropertyName(nameof(SoundBank));
            serializer.Serialize(writer, SoundBank);
        }
    }
}
