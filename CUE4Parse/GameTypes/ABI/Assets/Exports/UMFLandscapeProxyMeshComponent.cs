using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;

namespace CUE4Parse.GameTypes.ABI.Assets.Exports;

public class UMFLandscapeProxyMeshComponent : UMeshComponent
{
    public FMFLandscapeProxyMeshTile[] Tiles = [];
    public FVector[] Vertices = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        if (Ar.Game is not GAME_ArenaBreakoutInfinite || Ar.Position >= validPos)
            return;

        Tiles = Ar.ReadArray(() => new FMFLandscapeProxyMeshTile(Ar));
        Vertices = Ar.ReadBulkArray<FVector>();
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName(nameof(Tiles));
        serializer.Serialize(writer, Tiles);

        writer.WritePropertyName(nameof(Vertices));
        serializer.Serialize(writer, Vertices);
    }
}

public readonly struct FMFLandscapeProxyMeshTile
{
    public readonly FIntPoint Coordinates;
    public readonly FBox Bounds;
    public readonly int[] TriangleIndices;
    public readonly int[][] EdgeVertexIndices;

    public FMFLandscapeProxyMeshTile(FAssetArchive Ar)
    {
        Coordinates = Ar.Read<FIntPoint>();
        Bounds = new FBox(Ar);
        TriangleIndices = Ar.ReadBulkArray<int>();
        EdgeVertexIndices = Ar.ReadArray(4, Ar.ReadBulkArray<int>);
    }
}
