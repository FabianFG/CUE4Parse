using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.OtherGames.Objects;

public class UBiomesSpawnPointsGrid : UDataAsset
{
    public FVector2D[][] SpawnPoints = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        SpawnPoints = Ar.ReadArray(Ar.ReadBulkArray<FVector2D>);
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);
        writer.WritePropertyName(nameof(SpawnPoints));
        serializer.Serialize(writer, SpawnPoints);
    }
}
