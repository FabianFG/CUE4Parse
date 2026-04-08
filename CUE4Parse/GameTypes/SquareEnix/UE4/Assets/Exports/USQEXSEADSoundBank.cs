using System;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.SquareEnix.UE4.Assets.Exports;

public class USQEXSEADSoundBank : UObject
{
    public int SQEXFlags;
    public FByteBulkData? SQEXSoundBankData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Game is EGame.GAME_DragonQuestXI || Ar.ReadBoolean())
        {
            SQEXFlags = Ar.Read<int>();
            Ar.Position += SQEXFlags switch
            {
                1 => 24, // bulkData size at the start (int or long)
                3 => 28, // bulkData size at the start (int or long)
                17 => 4,
                _ => throw new ParserException("Unknown SQEXSEADS asset flags"),
            };
            SQEXSoundBankData = new FByteBulkData(Ar);
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(Type));
        writer.WriteValue(SQEXFlags);
    }
}
