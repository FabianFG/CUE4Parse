using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

[StructFallback]
public readonly struct FQuadrantSpawnTypeProperties
{
    public readonly EQuadrantSpawnType QuadrantSpawnType;
    public readonly FSectionLenghtsProperties[] SectionLenghts;

    public FQuadrantSpawnTypeProperties(FStructFallback fallback)
    {
        QuadrantSpawnType = fallback.GetOrDefault<EQuadrantSpawnType>(nameof(QuadrantSpawnType));
        SectionLenghts = fallback.GetOrDefault<FSectionLenghtsProperties[]>(nameof(SectionLenghts), []);
    }
}
