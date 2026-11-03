using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

[StructFallback]
public readonly struct FQuadrantSpawnCategories
{
    public readonly FName QuadrantSpawnTag;
    public readonly FQuadrantSpawnItem[] Items;

    public FQuadrantSpawnCategories(FStructFallback fallback)
    {
        QuadrantSpawnTag = fallback.GetOrDefault<FName>(nameof(QuadrantSpawnTag));
        Items = fallback.GetOrDefault<FQuadrantSpawnItem[]>(nameof(Items), []);
    }
}
