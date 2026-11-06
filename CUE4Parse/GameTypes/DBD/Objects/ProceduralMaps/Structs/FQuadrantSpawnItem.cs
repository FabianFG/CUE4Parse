using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

[StructFallback]
public readonly struct FQuadrantSpawnItem
{
    public readonly EQuadrantSpawnType Type;
    public readonly FWeightedItem[] Elements;

    public FQuadrantSpawnItem(FStructFallback fallback)
    {
        Type = fallback.GetOrDefault<EQuadrantSpawnType>(nameof(Type));
        Elements = fallback.GetOrDefault<FWeightedItem[]>(nameof(Elements), []);
    }
}
