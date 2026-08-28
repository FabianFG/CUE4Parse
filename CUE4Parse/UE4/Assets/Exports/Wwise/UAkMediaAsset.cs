using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Wwise;

public class UAkMediaAsset : UObject
{
    public uint ID { get; private set; }
    public string MediaName { get; private set; } = string.Empty;
    public FPackageIndex? CurrentMediaAssetData { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        ID = GetOrDefault<uint>(nameof(ID), comparisonType: StringComparison.OrdinalIgnoreCase);
        MediaName = GetOrDefault<string>(nameof(MediaName));
        CurrentMediaAssetData = new FPackageIndex(Ar);

        if (Ar.Game is GAME_GearsofWarEDay && GetOrDefault<byte>("TCCookedWithData") == 1)
        {
            Ar.Position += 2;
            var count = Ar.Read<int>();
            CustomGameData = new FByteBulkData(Ar);
            Ar.Position += count == 2 ? 16 : 8;
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (CurrentMediaAssetData is null) return;

        writer.WritePropertyName(nameof(CurrentMediaAssetData));
        serializer.Serialize(writer, CurrentMediaAssetData);
    }
}
