using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

[StructFallback]
public readonly struct FSectionLenghtsProperties
{
    public readonly EDBDDirection Direction;
    public readonly int Length;

    public FSectionLenghtsProperties(FStructFallback fallback)
    {
        Direction = fallback.GetOrDefault<EDBDDirection>(nameof(Direction));
        Length = fallback.GetOrDefault<int>(nameof(Length));
    }
}
