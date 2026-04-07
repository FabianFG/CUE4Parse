using System;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.SquareEnix.UE4.Assets.Exports;

public class USQEXSEADSound : USoundWave
{
    public FPackageIndex? ReferenceBank;
    public int SoundIndex;
    public int SQEXFlags;
    public FByteBulkData? SQEXSoundData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        ReferenceBank = GetOrDefault<FPackageIndex>(nameof(ReferenceBank));
        SoundIndex = GetOrDefault<int>(nameof(SoundIndex));
        if (Ar.ReadBoolean())
        {
            SQEXFlags = Ar.Read<int>();
            Ar.Position += SQEXFlags switch
            {
                1 => 24, // bulkData size at the start (int or long)
                3 => 28, // bulkData size at the start (int or long)
                17 => 8,
                _ => throw new ParserException("Unknown SQEXSEADS asset flags"),
            };
            SQEXSoundData = new FByteBulkData(Ar);
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(Type));
        writer.WriteValue(SQEXFlags);
    }
}
