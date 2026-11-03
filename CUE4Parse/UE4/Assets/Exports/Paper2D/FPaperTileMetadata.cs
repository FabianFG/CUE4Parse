using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.UE4.Assets.Exports.Paper2D;

[StructFallback]
public readonly struct FPaperTileMetadata
{
    public readonly FName UserDataName;
    public readonly FSpriteGeometryCollection CollisionData;
    public readonly byte[] TerrainMembership;

    public FPaperTileMetadata(FStructFallback fallback)
    {
        UserDataName = fallback.GetOrDefault<FName>(nameof(UserDataName));
        CollisionData = new FSpriteGeometryCollection(fallback.GetOrDefault(nameof(CollisionData), new FStructFallback()), ESpritePolygonMode.FullyCustom);
        TerrainMembership = [byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue];
        if (fallback.TryGetAllValues<byte>(out var membership, nameof(TerrainMembership)))
            Array.Copy(membership, TerrainMembership, Math.Min(membership.Length, TerrainMembership.Length));
    }

    public bool HasCollision() => CollisionData.Shapes.Length > 0;
    public bool HasMetaData() => !UserDataName.IsNone;
}
