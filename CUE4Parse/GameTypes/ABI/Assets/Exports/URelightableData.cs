using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.ABI.Assets.Exports;

public class URelightableData : UObject
{
    public int ShadowMaskHeader;
    public byte[]? ShadowMaskData = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Game is not GAME_ArenaBreakoutInfinite || Ar.Position >= validPos)
            return;

        ShadowMaskHeader = Ar.Read<int>();
        ShadowMaskData = Ar.ReadBulkArray<byte>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (ShadowMaskData is null)
            return;

        writer.WritePropertyName(nameof(ShadowMaskHeader));
        writer.WriteValue(ShadowMaskHeader);
    }
}
