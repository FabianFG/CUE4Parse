using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.DBD.Objects.ProceduralMaps.Structs;

[StructFallback]
public readonly struct FThemeProperties
{
    public readonly FName Name;
    public readonly string TilePath;
    public readonly string MapPath;

    public FThemeProperties(FStructFallback fallback) : this(fallback, default) { }

    public FThemeProperties(FStructFallback fallback, FName rowName)
    {
        Name = fallback.GetOrDefault<FName>(nameof(Name));
        if (Name.IsNone) Name = rowName;
        TilePath = fallback.GetOrDefault(nameof(TilePath), string.Empty);
        MapPath = fallback.GetOrDefault(nameof(MapPath), string.Empty);
    }
}
