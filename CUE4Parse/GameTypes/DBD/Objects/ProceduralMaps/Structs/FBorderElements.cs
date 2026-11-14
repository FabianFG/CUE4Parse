using CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Enums;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

[StructFallback]
public readonly struct FBorderElements(FStructFallback fallback)
{
    public readonly EDBDDirection Direction = fallback.GetOrDefault<EDBDDirection>(nameof(Direction));
    public readonly FPackageIndex[] Elements = fallback.GetOrDefault<FPackageIndex[]>(nameof(Elements), []);
}
