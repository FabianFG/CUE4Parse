using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;

namespace CUE4Parse.UE4.Assets.Exports.Paper2D;

public class UPaperTileLayer : UObject
{
    private static readonly FTransform[] TilePermutationTransforms =
    [
        FTransform.Identity,
        new(new FRotator(90, 0, 0), FVector.ZeroVector, new FVector(-1, 1, 1)),
        new(new FRotator(-180, 0, 0), FVector.ZeroVector, new FVector(-1, 1, 1)),
        new(new FRotator(90, 0, 0)),
        new(FRotator.ZeroRotator, FVector.ZeroVector, new FVector(-1, 1, 1)),
        new(new FRotator(-90, 0, 0)),
        new(new FRotator(-180, 0, 0)),
        new(new FRotator(-90, 0, 0), FVector.ZeroVector, new FVector(-1, 1, 1))
    ];

    public FText LayerName = new(string.Empty);
    public int LayerWidth = 4;
    public int LayerHeight = 4;
    public int AllocatedWidth = 4;
    public int AllocatedHeight = 4;
    public FPaperTileInfo[] AllocatedCells = [];

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        LayerName = GetOrDefault(nameof(LayerName), new FText(string.Empty));
        LayerWidth = GetOrDefault(nameof(LayerWidth), 4);
        LayerHeight = GetOrDefault(nameof(LayerHeight), 4);
        AllocatedWidth = GetOrDefault(nameof(AllocatedWidth), 4);
        AllocatedHeight = GetOrDefault(nameof(AllocatedHeight), 4);
        AllocatedCells = GetOrDefault<FPaperTileInfo[]>(nameof(AllocatedCells), []);
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < LayerWidth && y < LayerHeight;

    public FPaperTileInfo GetCell(int x, int y)
    {
        if (!InBounds(x, y))
            return new FPaperTileInfo();

        var index = (long) y * LayerWidth + x;
        return index < AllocatedCells.Length ? AllocatedCells[index] : new FPaperTileInfo();
    }

    public static FTransform GetTileTransform(int flagIndex) => TilePermutationTransforms[flagIndex];
}
