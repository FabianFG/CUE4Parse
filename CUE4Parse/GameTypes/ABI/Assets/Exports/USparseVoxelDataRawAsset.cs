using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.ABI.Assets.Exports;

public class USparseVoxelDataRawAsset : UObject
{
    public FByteBulkData? VoxelData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Game is not GAME_ArenaBreakoutInfinite || Ar.Position >= validPos)
            return;

        VoxelData = new FByteBulkData(Ar);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (VoxelData is null)
            return;

        writer.WritePropertyName(nameof(VoxelData));
        serializer.Serialize(writer, VoxelData);
    }
}
